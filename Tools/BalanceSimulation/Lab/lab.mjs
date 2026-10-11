import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import crypto from 'node:crypto';
import {spawn} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {project,store,jobsRoot,modelsRoot,jobFile,modelFile,read,atomic,safeId,inside,alive,preset,settings,runtime,execute,validateModel,episodeReport} from './core.mjs';
import {createJob,runJob} from './worker.mjs';
import {compareJobs} from './compare.mjs';
import {rebuildJobReports} from './reports.mjs';

const here=path.dirname(fileURLToPath(import.meta.url));
if(process.argv[2]==='worker'){await runJob(safeId(process.argv[3]));process.exit(0);}
if(process.argv[2]==='run'){const job=createJob(read(process.argv[3]));console.log('Job: '+job.id);await runJob(job.id);console.log(read(jobFile(job.id)).status);process.exit(read(jobFile(job.id)).status==='completed'?0:1);}
if(process.argv[2]==='rebuild-reports'){await rebuildJobReports(safeId(process.argv[3]));process.exit(0);}
const token=crypto.randomBytes(32).toString('hex'),port=Number(process.env.BALANCE_LAB_PORT||8877);
function launch(id){
 const job=read(jobFile(id));if(job.status==='completed')throw Error('Завершённый эксперимент неизменяем; создай новый');
 const lock=path.join(store,'worker.lock');if(fs.existsSync(lock)&&alive(read(lock).pid))throw Error('Уже идёт эксперимент');
 if(job.pid&&alive(job.pid))throw Error('Эта задача уже работает');
 const stop=path.join(path.dirname(jobFile(id)),'stop.request');if(fs.existsSync(stop))fs.unlinkSync(stop);
 job.status='queued';atomic(jobFile(id),job);
 const child=spawn(process.execPath,[fileURLToPath(import.meta.url),'worker',id],{cwd:here,windowsHide:true,stdio:'ignore',detached:false,env:process.env});
 child.on('error',error=>{const failed=read(jobFile(id));failed.status='failed';failed.error=error.message;atomic(jobFile(id),failed);});
 job.pid=child.pid;atomic(jobFile(id),job);return job;
}
function listJobs(){return fs.readdirSync(jobsRoot).flatMap(id=>{try{const job=read(jobFile(id));if(['preflight','sampling','evaluating','learning','publishing','queued'].includes(job.status)&&job.pid&&!alive(job.pid)){job.status='interrupted';job.error='Процесс прерван. Завершённые кампании сохранены; возобновление повторит незавершённую.';atomic(jobFile(id),job);}return[job];}catch{return[];}}).sort((a,b)=>b.createdAt.localeCompare(a.createdAt));}
function models(){return fs.readdirSync(modelsRoot).filter(f=>f.endsWith('.json')).map(file=>{const model=validateModel(read(path.join(modelsRoot,file)));return{id:model.id,parent:model.parent,episodes:model.episodes,updates:model.updates,contexts:Object.keys(model.memory||{}).length,createdAt:fs.statSync(path.join(modelsRoot,file)).mtime.toISOString()};}).sort((a,b)=>b.createdAt.localeCompare(a.createdAt));}
const featureNames=['Общий сдвиг (одинаков для всех действий)','Исходная полезность','Большая нода','Сокет','Бесконечная нода','Стоимость со знаком минус','Атака','Защита','Защита против физического урона','Защита против стихий','Защита против мистического урона','Барьер: наличие / отсутствие','Регенерация','Выбранный атрибут','Атака × шанс крита','Защита × броня против врага'];
function modelReports(id){const played=[],created=[];for(const j of listJobs())for(const e of j.epochs){const viewer=e.viewer||(e.index===j.epochs.length-1?j.viewer:null);if(!viewer)continue;const item={jobId:j.id,epoch:e.index+1,mode:j.config.mode,viewer,inputModel:e.inputModel,outputModel:e.outputModel};if(e.inputModel===id)played.push(item);if(e.outputModel===id)created.push(item);}return {played,created};}
function memoryLabels(model){
 const labels=new Map(),job=listJobs().find(j=>j.modelId===model.id||j.config.modelId===model.id||j.epochs.some(e=>e.inputModel===model.id||e.outputModel===model.id));
 const episode=job?.epochs.flatMap(e=>e.episodes).find(e=>e?.report);if(!episode)return labels;
 const data=episodeReport(job,episode);
 for(const node of data.treeCatalog||[])labels.set('node:'+node.id,[node.type,...node.modifiers.map(m=>m.stat?`${m.operation} ${m.stat} ${m.value}${m.levelsPerModifier?' / '+m.levelsPerModifier+' lvl':''}`:m.name)].join('; '));
 for(const action of data.campaigns[0].agentLog||[])for(const choice of action.choices)if(!labels.has(choice.id))labels.set(choice.id,choice.label);
 return labels;
}
function send(res,status,value){res.writeHead(status,{'Content-Type':'application/json; charset=utf-8','Cache-Control':'no-store'});res.end(JSON.stringify(value));}
async function body(req){let data='';for await(const chunk of req){data+=chunk;if(data.length>64_000)throw Error('Слишком большой запрос');}return JSON.parse(data||'{}');}
const server=http.createServer(async(req,res)=>{
 try{
  if(req.headers.host!==`127.0.0.1:${port}`)return send(res,403,{error:'Локальный адрес не совпадает'});
  const url=new URL(req.url,`http://127.0.0.1:${port}`);
  if(req.method==='GET'&&url.pathname==='/'){res.writeHead(200,{'Content-Type':'text/html; charset=utf-8','Cache-Control':'no-store','Content-Security-Policy':"default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'"});return res.end(fs.readFileSync(path.join(here,'index.html'),'utf8').replace('__TOKEN__',token));}
  if(req.method==='GET'&&url.pathname.startsWith('/files/')){
   const parts=decodeURIComponent(url.pathname.slice(7)).split('/');safeId(parts[0]);const root=path.dirname(jobFile(parts.shift())),file=inside(root,parts.join('/'));
   const real=fs.realpathSync(file);if(!real.startsWith(fs.realpathSync(root)+path.sep)||!fs.statSync(real).isFile())throw Error('Недоступный файл');
   const ext=path.extname(real);if(!['.html','.md','.json','.gz'].includes(ext))throw Error('Недоступный тип файла');
   res.writeHead(200,{'Content-Type':ext==='.html'?'text/html; charset=utf-8':ext==='.json'?'application/json':'application/octet-stream','X-Content-Type-Options':'nosniff'});fs.createReadStream(real).pipe(res);return;
  }
  if(!url.pathname.startsWith('/api/'))return send(res,404,{error:'Не найдено'});
  if(req.headers['x-lab-token']!==token)return send(res,403,{error:'Недействительный ключ локального приложения'});
  if(req.method==='GET'&&url.pathname==='/api/state'){const saved=path.join(store,'last-settings.json');return send(res,200,{jobs:listJobs(),models:models(),strategies:preset().strategies,defaults:fs.existsSync(saved)?settings(read(saved)):settings({}),coverage:'Проверены базовый контракт, повторяемость и четыре Play/Edit сценария. Все ноды допускаются, но исчерпывающая проверка механик ещё не выполнена. Гемы: числовые LocalModifiers.'});}
  if(req.method==='GET'&&url.pathname.startsWith('/api/model/')){const model=validateModel(read(modelFile(safeId(url.pathname.split('/').at(-1)))));const labels=memoryLabels(model);const memory=Object.entries(model.memory||{}).sort((a,b)=>b[1].count-a[1].count).slice(0,100).map(([key,value])=>({key,context:key.slice(0,key.lastIndexOf('/')),action:labels.get(key.slice(key.lastIndexOf('/')+1))||key.slice(key.lastIndexOf('/')+1),...value,offset:model.offsets[key]||0}));const parent=model.parent?validateModel(read(modelFile(model.parent))):null;return send(res,200,{id:model.id,parent:model.parent,algorithm:model.algorithm,episodes:model.episodes,updates:model.updates,weights:model.weights,weightRows:model.weights.map((value,i)=>({feature:featureNames[i],value,delta:parent?value-parent.weights[i]:null})),reports:modelReports(model.id),memory});}
  if(req.method!=='POST')return send(res,405,{error:'Недопустимый метод'});
  if(req.headers.origin&&req.headers.origin!==`http://127.0.0.1:${port}`)return send(res,403,{error:'Внешний сайт не может запускать эксперименты'});
  const input=await body(req);
  if(url.pathname==='/api/settings'){const config=settings(input);atomic(path.join(store,'last-settings.json'),config);return send(res,200,{saved:true});}
  if(url.pathname==='/api/start'){const job=createJob(input);return send(res,200,launch(job.id));}
  if(url.pathname==='/api/resume')return send(res,200,launch(safeId(input.id)));
  if(url.pathname==='/api/stop'){const job=read(jobFile(input.id));if(job.status==='completed')throw Error('Эксперимент уже завершён');atomic(path.join(path.dirname(jobFile(input.id)),'stop.request'),'stop');return send(res,200,{requested:true});}
  if(url.pathname==='/api/compare')return send(res,200,compareJobs(safeId(input.left),safeId(input.right)));
  if(url.pathname==='/api/preflight'){const result=JSON.parse(await execute(runtime('unity'),['status','--format','json']));return send(res,200,{ready:!!(result.success&&result.data.instances.some(i=>path.resolve(i.project).toLowerCase()===project.toLowerCase()&&i.state==='ready'))});}
  send(res,404,{error:'Не найдено'});
 }catch(error){send(res,400,{error:error.message});}
});
server.on('error',error=>{console.error(error.message);process.exit(1);});
server.listen(port,'127.0.0.1',()=>console.log(`Balance Lab: http://127.0.0.1:${port}`));
