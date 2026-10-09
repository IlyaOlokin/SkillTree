import fs from 'node:fs';
import path from 'node:path';
import {jobFile,read,episodeReport,digest,mean,atomic,store} from './core.mjs';

export function compareJobs(leftId,rightId){
 const a=read(jobFile(leftId)),b=read(jobFile(rightId));
 if(a.status!=='completed'||b.status!=='completed'||a.config.mode!=='evaluate'||b.config.mode!=='evaluate')throw Error('Сравниваются только завершённые проверки с замороженной политикой');
 if(a.scenarioHash!==b.scenarioHash||a.environment.engine!==b.environment.engine)throw Error('Настройки сценария или версия симулятора различаются — повторяемое сравнение невозможно');
 const sameContent=a.environment.content===b.environment.content,samePolicy=a.config.policy===b.config.policy&&a.modelId===b.modelId;
 if(!sameContent&&!samePolicy)throw Error('Одновременно изменились игра и агент. Зафиксируй одну из них для контролируемого сравнения');
 const rows=job=>job.epochs.flatMap(e=>e.episodes.map(p=>episodeReport(job,p).campaigns[0]));
 const left=rows(a),right=rows(b),byKey=new Map(right.map(r=>[r.strategy+'/'+r.seed,r]));
 if(left.length!==right.length||left.some(r=>!byKey.has(r.strategy+'/'+r.seed)))throw Error('Наборы strategy/seed не совпадают');
 let randomState=47291;const random=()=>{randomState^=randomState<<13;randomState^=randomState>>>17;randomState^=randomState<<5;return(randomState>>>0)/4294967296;};
 const groups=[...new Set(left.map(r=>r.strategy))].map(strategy=>{
  const cohort=left.filter(r=>r.strategy===strategy),differences=cohort.map(r=>byKey.get(strategy+'/'+r.seed).highestCompletedStage-r.highestCompletedStage);
  let interval=null;if(differences.length>=5){const samples=[];for(let n=0;n<5000;n++){let total=0;for(let i=0;i<differences.length;i++)total+=differences[Math.floor(random()*differences.length)];samples.push(total/differences.length);}samples.sort((x,y)=>x-y);interval=[samples[125],samples[4874]];}
  const ordered=[...differences].sort((x,y)=>x-y),middle=Math.floor(ordered.length/2);
  return {strategy,runs:cohort.length,leftMean:mean(cohort.map(r=>r.highestCompletedStage)),rightMean:mean(cohort.map(r=>byKey.get(strategy+'/'+r.seed).highestCompletedStage)),pairedDelta:mean(differences),medianDelta:ordered.length%2?ordered[middle]:(ordered[middle-1]+ordered[middle])/2,improved:differences.filter(d=>d>0).length,worse:differences.filter(d=>d<0).length,unchanged:differences.filter(d=>d===0).length,bootstrap95:interval,insufficientSample:differences.length<20,exactGameplayMatches:sameContent&&samePolicy?cohort.filter(r=>digest(r)===digest(byKey.get(strategy+'/'+r.seed))).length:null,pairs:cohort.map((r,i)=>({seed:r.seed,before:r.highestCompletedStage,after:byKey.get(strategy+'/'+r.seed).highestCompletedStage,delta:differences[i]}))};
 });
 const result={id:digest([leftId,rightId]).slice(0,24),left:leftId,right:rightId,type:sameContent?(samePolicy?'repeatability':'agent-policy'):'game-balance',groups,warning:'Парный bootstrap — ориентировочная неопределённость. Малый набор seeds не доказывает улучшение; тест на зависимых повторных запусках не заменяет новые seeds.'};
 atomic(path.join(store,'comparisons',result.id+'.json'),result);return result;
}
