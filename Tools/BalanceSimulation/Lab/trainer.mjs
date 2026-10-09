import {FEATURES,validateModel,mean} from './core.mjs';

// Monte-Carlo policy gradient with a state-bucket baseline. No updates during
// an episode; epsilon-mixture derivative matches the C# sampling distribution.
export function train(parent,bots,config){
 validateModel(parent);const model=structuredClone(parent);delete model.id;model.parent=parent.id;
 const gradient=Array(FEATURES).fill(0),offsets={},baselineSamples={},memorySamples={};let used=0,decisions=0;
 for(const bot of bots){
  const logs=bot.agentLog||[],events=bot.agentRewards||[];if(!logs.length||!events.length)throw Error('Нет траекторий для обучения');
  const episodeGradient=Array(FEATURES).fill(0),episodeOffsets={};let count=0;
  for(let index=0;index<logs.length;index++){
   const action=logs[index];if(action.modelId!==parent.id)throw Error('Траектория записана другой моделью');
   const future=events.filter(e=>e.cursor>index);let value=0,discount=1;for(const event of future){value+=discount*event.amount;discount*=config.gamma;}
   const bucket=bot.strategy+'/'+Math.floor(action.stage/20),baseline=parent.baselines[bucket]?.mean||0;
   (baselineSamples[bucket]??=[]).push(value);
   const selected=action.choices[action.selected];if(!selected||selected.x.length!==FEATURES||action.choices.some(c=>c.x.some(v=>!Number.isFinite(v))))throw Error('Повреждённая траектория');
   (memorySamples[selected.key]??=[]).push(value);
   if(action.choices.length<2||!future.length)continue;
   const advantage=Math.max(-5,Math.min(5,(value-baseline)/Math.max(1,Math.abs(baseline),config.levelReward*10)));
   const factor=(1-action.exploration)*selected.softmax/selected.probability/action.temperature;
   for(let choice=0;choice<action.choices.length;choice++){
    const candidate=action.choices[choice],coefficient=advantage*factor*((choice===action.selected?1:0)-candidate.softmax);
    for(let feature=0;feature<FEATURES;feature++)episodeGradient[feature]+=coefficient*candidate.x[feature];
    episodeOffsets[candidate.key]=(episodeOffsets[candidate.key]||0)+coefficient;
   }
   count++;decisions++;
  }
  if(count){for(let i=0;i<FEATURES;i++)gradient[i]+=episodeGradient[i]/count;for(const [key,g]of Object.entries(episodeOffsets))offsets[key]=(offsets[key]||0)+g/count;used++;}
  const seedKey=bot.strategy+'/'+bot.seed;if(!model.trainingSeeds.includes(seedKey))model.trainingSeeds.push(seedKey);
 }
 if(!used)throw Error('Нет решений с альтернативами и будущими наградами');
 const norm=Math.sqrt(gradient.reduce((s,g)=>s+g*g,0)+Object.values(offsets).reduce((s,g)=>s+g*g,0))/used;
 const scale=config.learningRate/used/Math.max(1,norm);
 for(let i=0;i<FEATURES;i++)model.weights[i]=Math.max(-10,Math.min(10,model.weights[i]+gradient[i]*scale));
 for(const [key,g]of Object.entries(offsets))model.offsets[key]=Math.max(-8,Math.min(8,(model.offsets[key]||0)+g*scale));
 for(const [key,values]of Object.entries(baselineSamples)){const old=model.baselines[key]||{count:0,mean:0};model.baselines[key]={count:old.count+values.length,mean:(old.mean*old.count+values.reduce((a,b)=>a+b,0))/(old.count+values.length)};}
 for(const [key,values]of Object.entries(memorySamples)){const old=model.memory[key]||{count:0,meanReturn:0};model.memory[key]={count:old.count+values.length,meanReturn:(old.meanReturn*old.count+values.reduce((a,b)=>a+b,0))/(old.count+values.length)};}
 model.episodes+=bots.length;model.updates++;
 validateModel(model);return {model,statistics:{episodes:bots.length,decisions,gradientNorm:norm,rewardMean:mean(bots.map(b=>b.agentRewards.reduce((s,e)=>s+e.amount,0)))}};
}
