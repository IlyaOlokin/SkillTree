import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import {fileURLToPath} from 'node:url';
import {spawn, execFileSync} from 'node:child_process';

export const toolsRoot=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
export const project=path.resolve(toolsRoot,'../..');
export const store=path.join(project,'Reports/BalanceSimulation/Lab');
export const jobsRoot=path.join(store,'jobs'), modelsRoot=path.join(store,'models');
export const FEATURES=16;
for(const dir of [store,jobsRoot,modelsRoot])fs.mkdirSync(dir,{recursive:true});
export function canonical(value){if(Array.isArray(value))return value.map(canonical);if(value&&typeof value==='object')return Object.fromEntries(Object.keys(value).sort().map(k=>[k,canonical(value[k])]));return value;}
export const digest=value=>crypto.createHash('sha256').update(typeof value==='string'||Buffer.isBuffer(value)?value:JSON.stringify(canonical(value))).digest('hex');
export function atomic(file,value){fs.mkdirSync(path.dirname(file),{recursive:true});const tmp=file+'.'+crypto.randomUUID()+'.tmp';try{fs.writeFileSync(tmp,typeof value==='string'?value:JSON.stringify(value,null,2));fs.renameSync(tmp,file);}finally{if(fs.existsSync(tmp))fs.unlinkSync(tmp);}}
export function read(file){return JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));}
export function report(file){return JSON.parse(zlib.gunzipSync(fs.readFileSync(file)).toString('utf8'));}
export function safeId(id){if(typeof id!=='string'||!/^[a-zA-Z0-9_-]{1,100}$/.test(id))throw Error('Некорректный идентификатор');return id;}
export const jobFile=id=>path.join(jobsRoot,safeId(id),'job.json');
export const modelFile=id=>path.join(modelsRoot,safeId(id)+'.json');
export function episodeReport(job,episode){const file=inside(path.dirname(jobFile(job.id)),episode.report),bytes=fs.readFileSync(file);if(episode.sha256&&digest(bytes)!==episode.sha256)throw Error('Завершённый отчёт изменён — результат нельзя использовать');const data=report(file);if(data.isPartial||data.campaigns.length!==1||data.campaigns[0].seed!==episode.seed||data.campaigns[0].strategy!==episode.strategy||data.campaigns[0].status==='blocked')throw Error('Неполный или некорректный результат кампании');return data;}
export function inside(root,file){const resolved=path.resolve(root,file);if(!resolved.startsWith(path.resolve(root)+path.sep))throw Error('Путь за пределами результатов');return resolved;}
export function alive(pid){try{process.kill(pid,0);return true;}catch{return false;}}
export function runtime(name){const env=name==='pwsh'?process.env.BALANCE_PWSH:process.env.BALANCE_UNITY;if(env&&fs.existsSync(env))return env;try{return execFileSync('where.exe',[name],{encoding:'utf8',windowsHide:true}).trim().split(/\r?\n/)[0];}catch{throw Error(`Не найден ${name}. Запускай через StartLab.ps1 или укажи путь в окружении.`);}}
export function execute(exe,args,onLine=()=>{},env={}){return new Promise((resolve,reject)=>{const child=spawn(exe,args,{cwd:project,windowsHide:true,env:{...process.env,...env},stdio:['ignore','pipe','pipe']});let output='';for(const stream of [child.stdout,child.stderr])stream.on('data',chunk=>{const text=chunk.toString();output+=text;onLine(text);if(output.length>2_000_000)output=output.slice(-1_000_000);});child.on('error',reject);child.on('close',code=>code===0?resolve(output):reject(Error(output.trim()||`Процесс завершился с кодом ${code}`)));});}
export const preset=()=>read(path.join(toolsRoot,'Presets/balanced-shops.json'));
function number(value,name,min,max,integer=false){if(typeof value!=='number'||!Number.isFinite(value)||value<min||value>max||(integer&&!Number.isInteger(value)))throw Error(`Настройка ${name}: допустимо ${min}–${max}`);return value;}
export function settings(input){
 const base={mode:'evaluate',policy:'heuristic',modelId:null,strategies:preset().strategies.map(s=>s.name),runsPerStrategy:5,epochs:3,seed:101,locations:20,maxAttemptsPerStage:20,maxWaveSeconds:180,temperature:1,exploration:0.1,learningRate:0.02,gamma:0.99,levelReward:1,locationReward:5,failurePenalty:0.1};
 const value={...base,...input};if(!['evaluate','train'].includes(value.mode)||!['heuristic','learned'].includes(value.policy))throw Error('Неизвестный режим');
 if(value.mode==='train')value.policy='learned';
 const allowed=preset().strategies.map(s=>s.name);if(!Array.isArray(value.strategies)||!value.strategies.length||new Set(value.strategies).size!==value.strategies.length||value.strategies.some(s=>!allowed.includes(s)))throw Error('Выбери доступные стратегии');
 for(const [key,min,max,int]of [['runsPerStrategy',1,1000,true],['epochs',1,100,true],['seed',0,2_000_000_000,true],['locations',2,20,true],['maxAttemptsPerStage',1,20,true],['maxWaveSeconds',1,3600,true],['temperature',0.1,10],['exploration',0,0.9],['learningRate',0.00001,0.2],['gamma',0.5,1],['levelReward',0.01,100],['locationReward',0,100],['failurePenalty',0,100]])number(value[key],key,min,max,int);
 if(value.seed+value.runsPerStrategy*value.epochs>2_000_000_000)throw Error('Диапазон seeds слишком велик');
 if(value.modelId){safeId(value.modelId);if(!fs.existsSync(modelFile(value.modelId)))throw Error('Модель не найдена');}
 return Object.fromEntries(Object.keys(base).map(k=>[k,value[k]]));
}
export function bootstrap(){return {schema:'balance-actor-v1',featureVersion:1,algorithm:'residual-linear-reinforce',weights:Array(FEATURES).fill(0),offsets:{},baselines:{},memory:{},trainingSeeds:[],episodes:0,updates:0,parent:null};}
export function validateModel(model){if(model.schema!=='balance-actor-v1'||model.featureVersion!==1||!Array.isArray(model.weights)||model.weights.length!==FEATURES||model.weights.some(v=>!Number.isFinite(v))||!Array.isArray(model.trainingSeeds))throw Error('Несовместимая модель');for(const v of Object.values(model.offsets||{}))if(!Number.isFinite(v))throw Error('Повреждённая память модели');if(model.id){const payload=structuredClone(model);delete payload.id;if(digest(payload).slice(0,32)!==model.id)throw Error('Контрольная сумма модели не совпадает — файл изменён');}return model;}
export function saveModel(model){const copy=structuredClone(model);delete copy.id;copy.id=digest(copy).slice(0,32);const file=modelFile(copy.id);if(!fs.existsSync(file)){fs.writeFileSync(file,JSON.stringify(copy,null,2),{flag:'wx'});}else if(digest(read(file))!==digest(copy))throw Error('Коллизия версии модели');return copy;}
export function fingerprint(){
 const game=crypto.createHash('sha256'),engine=crypto.createHash('sha256');let files=0;
 function walk(root,hash,filter){if(!fs.existsSync(root))return;for(const item of fs.readdirSync(root,{withFileTypes:true}).sort((a,b)=>a.name.localeCompare(b.name))){const file=path.join(root,item.name);if(item.isSymbolicLink())throw Error('Ссылки в контролируемых входных данных не поддерживаются');if(item.isDirectory())walk(file,hash,filter);else if(filter(file)){hash.update(path.relative(project,file).replaceAll('\\','/'));hash.update(fs.readFileSync(file));files++;}}}
 walk(path.join(project,'Assets'),game,f=>/\.(cs|asset|prefab|unity|json|csv|meta)$/i.test(f));
 walk(path.join(project,'ProjectSettings'),game,()=>true);walk(path.join(project,'Packages'),game,f=>/\.(json|asmdef|cs)$/i.test(f));
 walk(path.join(toolsRoot,'Runtime'),engine,f=>f.endsWith('.cs')&&!path.basename(f).startsWith('Parity'));
 for(const file of ['RunCampaign.ps1','Internal/UnityBridge.ps1'])engine.update(fs.readFileSync(path.join(toolsRoot,file)));
 engine.update(fs.readFileSync(fileURLToPath(import.meta.url)));for(const file of ['trainer.mjs','worker.mjs'])engine.update(fs.readFileSync(path.join(path.dirname(fileURLToPath(import.meta.url)),file)));
 return {content:game.digest('hex'),engine:engine.digest('hex'),files,unity:fs.readFileSync(path.join(project,'ProjectSettings/ProjectVersion.txt'),'utf8').trim()};
}
export const mean=values=>values.reduce((a,b)=>a+b,0)/values.length;
export function aggregate(rows){return [...new Set(rows.map(r=>r.strategy))].map(strategy=>{const group=rows.filter(r=>r.strategy===strategy),stages=group.map(r=>r.highestCompletedStage).sort((a,b)=>a-b),mid=Math.floor(stages.length/2);return {strategy,runs:group.length,mean:mean(stages),median:stages.length%2?stages[mid]:(stages[mid-1]+stages[mid])/2,min:stages[0],max:stages.at(-1),deaths:mean(group.map(r=>r.deaths)),routeCompletion:group.filter(r=>r.status==='completed').length/group.length,seeds:group.map(r=>r.seed),stages};});}
