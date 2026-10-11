import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {toolsRoot,project,store,jobsRoot,jobFile,modelFile,read,atomic,report,episodeReport,settings,preset,fingerprint,digest,runtime,execute,alive,bootstrap,saveModel,validateModel,aggregate} from './core.mjs';
import {train} from './trainer.mjs';
import {publishEpoch} from './reports.mjs';

export function createJob(input){const config=settings(input),id=new Date().toISOString().replace(/[-:.TZ]/g,'').slice(0,17)+'-'+crypto.randomUUID().slice(0,8);const job={schema:1,id,createdAt:new Date().toISOString(),status:'queued',config,epochs:[],completed:0,total:config.strategies.length*config.runsPerStrategy*(config.mode==='train'?config.epochs:1),error:null};atomic(jobFile(id),job);return job;}
const lock=path.join(store,'worker.lock');
function acquire(id){try{fs.writeFileSync(lock,JSON.stringify({pid:process.pid,id}),{flag:'wx'});}catch(error){if(fs.existsSync(lock)&&!alive(read(lock).pid)){fs.unlinkSync(lock);return acquire(id);}throw Error('Другой эксперимент уже использует Unity. Дождись завершения или останови его.');}}
export async function runJob(id){
 acquire(id);let job=read(jobFile(id));const root=path.dirname(jobFile(id)),stop=path.join(root,'stop.request'),log=path.join(root,'worker.log');
 function update(fields={}){Object.assign(job,fields,{updatedAt:new Date().toISOString()});atomic(jobFile(id),job);}
 function output(text){fs.appendFileSync(log,text);update({lastMessage:text.trim().slice(-1500)});}
 function cancelled(){return fs.existsSync(stop);}
 try{
  job.config=settings(job.config);update({status:'preflight',pid:process.pid,error:null});
  const unity=runtime('unity'),pwsh=runtime('pwsh');
  const status=JSON.parse(await execute(unity,['status','--format','json']));
  if(!status.success||!status.data.instances.some(i=>path.resolve(i.project).toLowerCase()===project.toLowerCase()&&i.state==='ready'))throw Error('Unity/Pipeline не готов. Открой MainScene в Edit Mode; при необходимости Window → Pipeline → Settings → Restart.');
  const environment=fingerprint();if(job.environment&&(job.environment.content!==environment.content||job.environment.engine!==environment.engine))throw Error('Игра или симулятор изменились. Этот эксперимент нельзя возобновить; создай новый.');
  if(!job.environment){job.environment=environment;job.scenarioHash=digest({...job.config,mode:undefined,policy:undefined,modelId:undefined,epochs:undefined,learningRate:undefined,exploration:undefined,temperature:undefined,gamma:undefined});update();}
  let model=null;if(job.config.policy==='learned')model=job.config.modelId?validateModel(read(modelFile(job.config.modelId))):saveModel(bootstrap());
  if(job.config.mode==='evaluate'&&model){for(let n=0;n<job.config.runsPerStrategy;n++)for(const strategy of job.config.strategies)if(model.trainingSeeds.includes(strategy+'/'+(job.config.seed+n)))throw Error('Проверочные seeds пересекаются с обучением этой модели. Выбери другие seeds.');}
  const epochCount=job.config.mode==='train'?job.config.epochs:1;
  for(let epoch=0;epoch<epochCount;epoch++){
   if(cancelled()){update({status:'cancelled'});return;}
   if(!job.epochs[epoch]){job.epochs[epoch]={index:epoch,inputModel:model?.id||null,episodes:[],outputModel:null};update();}
   const batch=job.epochs[epoch];if(batch.outputModel){model=validateModel(read(modelFile(batch.outputModel)));if(!batch.viewer||!fs.existsSync(path.join(root,batch.viewer))){batch.viewer=await publishEpoch(job,epoch,{pwsh,output});update();}continue;}
   if(batch.inputModel!==model?.id&&!(batch.inputModel===null&&model===null))throw Error('Нарушена последовательность версий модели');
   let ordinal=0;
   for(const strategy of job.config.strategies)for(let run=0;run<job.config.runsPerStrategy;run++,ordinal++){
    if(batch.episodes[ordinal]?.report){episodeReport(job,batch.episodes[ordinal]);continue;}
    if(cancelled()){update({status:'cancelled'});return;}
    const current=fingerprint();if(current.content!==environment.content||current.engine!==environment.engine)throw Error('Входные файлы изменились во время эксперимента. Результаты сохранены, дальнейшее обучение остановлено.');
    const seed=job.config.seed+epoch*job.config.runsPerStrategy+run;
    if(model)validateModel(read(modelFile(model.id)));
    const episodeRoot=path.join(root,`epoch-${epoch+1}`,`episode-${ordinal+1}`,crypto.randomUUID().slice(0,8));fs.mkdirSync(episodeRoot,{recursive:true});
    const config=preset();config.strategies=config.strategies.filter(s=>s.name===strategy);config.locationIds=config.locationIds.slice(0,job.config.locations);config.maxAttemptsPerStage=job.config.maxAttemptsPerStage;config.maxWaveSeconds=job.config.maxWaveSeconds;config.controlledRun=true;
    if(model)config.agent={modelPath:modelFile(model.id),training:job.config.mode==='train',temperature:job.config.temperature,exploration:job.config.exploration,levelReward:job.config.levelReward,locationReward:job.config.locationReward,failurePenalty:job.config.failurePenalty};
    atomic(path.join(episodeRoot,'preset.json'),config);update({status:job.config.mode==='train'?'sampling':'evaluating',current:{epoch:epoch+1,strategy,seed,model:model?.id||'heuristic'},lastMessage:'Запуск кампании'});
    try{await execute(pwsh,['-NoProfile','-File',path.join(toolsRoot,'RunCampaign.ps1'),'-Preset',path.join(episodeRoot,'preset.json'),'-RunsPerStrategy','1','-Seed',String(seed),'-UnityCli',unity,'-Output',path.join(episodeRoot,'report.json'),'-StopFile',stop],output);}catch(error){if(cancelled()){update({status:'cancelled'});return;}throw error;}
    const after=fingerprint();if(after.content!==environment.content||after.engine!==environment.engine)throw Error('Файлы изменились во время кампании. Этот результат не допущен в обучение или сравнение.');
    const data=report(path.join(episodeRoot,'report.json.gz'));if(data.isPartial||data.campaigns.length!==1||data.campaigns[0].seed!==seed||data.campaigns[0].strategy!==strategy||data.campaigns[0].status==='blocked')throw Error('Неполный или некорректный результат кампании');
    if(model)validateModel(read(modelFile(model.id)));
    batch.episodes[ordinal]={strategy,seed,report:path.relative(root,path.join(episodeRoot,'report.json.gz')),sha256:digest(fs.readFileSync(path.join(episodeRoot,'report.json.gz'))),stage:data.campaigns[0].highestCompletedStage};job.completed=job.epochs.reduce((sum,e)=>sum+e.episodes.filter(Boolean).length,0);update();
   }
   const datasets=batch.episodes.map(e=>episodeReport(job,e)),bots=datasets.flatMap(d=>d.campaigns);
   atomic(path.join(root,`epoch-${epoch+1}`,'summary.json'),aggregate(bots));
   if(job.config.mode==='train'){
    update({status:'learning'});const result=train(model,bots,job.config);result.model.trainingEnvironment=environment;result.model.trainingSettings={gamma:job.config.gamma,levelReward:job.config.levelReward,locationReward:job.config.locationReward,failurePenalty:job.config.failurePenalty};
    model=saveModel(result.model);batch.outputModel=model.id;batch.learning=result.statistics;update({latestModel:model.id});
   }
   update({status:'publishing',lastMessage:`Отчёт партии ${epoch+1}`});
   batch.viewer=await publishEpoch(job,epoch,{pwsh,output});update();
  }
  const rows=job.epochs.flatMap(e=>e.episodes.map(p=>episodeReport(job,p).campaigns[0]));
  job.summary=aggregate(rows);job.modelId=model?.id||null;update({status:'publishing',current:null,lastMessage:'Подготовка отчёта'});
  job.viewer=await publishEpoch(job,job.epochs.length-1,{final:true,pwsh,output});
  update({status:'completed',lastMessage:'Отчёт готов'});
 }catch(error){update({status:'failed',error:error.message,lastMessage:'Эксперимент остановлен; завершённые кампании сохранены'});fs.appendFileSync(log,'\nERROR: '+error.stack+'\n');}
 finally{if(fs.existsSync(lock)&&read(lock).pid===process.pid)fs.unlinkSync(lock);}
}
