import fs from 'node:fs';
import path from 'node:path';
import {jobFile,modelFile,read,atomic,episodeReport,validateModel,toolsRoot,execute,runtime} from './core.mjs';

export function modelUpdate(before,after){
 const weights=after.weights.map((v,i)=>v-before.weights[i]);
 const keys=new Set([...Object.keys(before.offsets),...Object.keys(after.offsets)]);
 const offsets=[...keys].map(k=>(after.offsets[k]||0)-(before.offsets[k]||0));
 return {weightMaxDelta:Math.max(0,...weights.map(Math.abs)),weightL2Delta:Math.sqrt(weights.reduce((s,v)=>s+v*v,0)),weightsChanged:weights.filter(v=>Math.abs(v)>1e-12).length,offsetMaxDelta:Math.max(0,...offsets.map(Math.abs)),offsetsChanged:offsets.filter(v=>Math.abs(v)>1e-12).length};
}

export async function publishEpoch(job,index,{final=false,pwsh=runtime('pwsh'),output=()=>{}}={}){
 const root=path.dirname(jobFile(job.id)),epoch=job.epochs[index];
 const assembled=epoch.episodes.map(p=>episodeReport(job,p));
 if(assembled.length!==job.config.strategies.length*job.config.runsPerStrategy)throw Error('Отчёт партии требует все завершённые кампании');
 const directory=final?root:path.join(root,`epoch-${index+1}`);
 const input=path.join(directory,'aggregate-input.json');
 const strategies=[...new Map(assembled.flatMap(d=>d.preset.strategies).map(s=>[s.name,s])).values()];
 atomic(input,{campaigns:assembled.flatMap(d=>d.campaigns),treeCatalog:assembled[0].treeCatalog,preset:{...assembled[0].preset,strategies}});
 await execute(pwsh,['-NoProfile','-File',path.join(toolsRoot,'Lab/PublishJob.ps1'),'-JobDirectory',root,'-Epoch',String(index+1),'-InputFile',input,'-OutputFile',path.join(directory,'report.json')],output);
 return path.relative(root,path.join(directory,'report.html')).replaceAll('\\','/');
}

export async function rebuildJobReports(id){
 const job=read(jobFile(id));
 if(job.status!=='completed')throw Error('Пересборка доступна только для завершённых задач');
 const beforeConfig=JSON.stringify(job.config);
 for(let index=0;index<job.epochs.length;index++){
  const e=job.epochs[index];
  if(e.outputModel){const before=validateModel(read(modelFile(e.inputModel))),after=validateModel(read(modelFile(e.outputModel)));e.learning={...e.learning,...modelUpdate(before,after)};}
  e.viewer=await publishEpoch(job,index);atomic(jobFile(id),job);
  console.log(`Published epoch ${index+1}/${job.epochs.length}`);
 }
 job.viewer=await publishEpoch(job,job.epochs.length-1,{final:true});
 if(JSON.stringify(job.config)!==beforeConfig)throw Error('Настройки изменились при пересборке');
 atomic(jobFile(id),job);return job;
}
