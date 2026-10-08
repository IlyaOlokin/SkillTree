// Embedded inside the report's async function; no external requests or libraries.
if (report.campaigns?.length) {
  const mean = values => values.reduce((sum, value) => sum + value, 0) / values.length;
  const groups = new Map();
  for (const bot of report.campaigns) {
    const final = [...(bot.snapshots || [])].reverse().find(s => !s.probe);
    if (!final) continue;
    if (!groups.has(bot.strategy)) groups.set(bot.strategy, []);
    groups.get(bot.strategy).push({bot, final});
  }
  let cohort = [], heatNodes = new Map(), meanRows = [], heatView = [0, 0, 100, 100], heatDrag = null;
  const heat = $('heatTree');
  function svgElement(tag, attrs) {
    const element = document.createElementNS(svgNS, tag);
    for (const [key, value] of Object.entries(attrs)) element.setAttribute(key, value);
    return element;
  }
  function heatColor(frequency) {
    if (!frequency) return '#3e4b60';
    const low = [59, 130, 246], high = [250, 204, 21];
    return `rgb(${low.map((value, i) => Math.round(value + (high[i] - value) * frequency)).join(',')})`;
  }
  function updateHeatView() { heat.setAttribute('viewBox', heatView.join(' ')); }
  function heatZoom(factor) {
    heatView = [heatView[0] + heatView[2] * (1 - factor) / 2, heatView[1] + heatView[3] * (1 - factor) / 2, heatView[2] * factor, heatView[3] * factor];
    updateHeatView();
  }
  function inspectHeatNode(id) {
    const node = byId.get(id), counts = heatNodes.get(id) || {allocated: 0, active: 0, seeds: []};
    $('heatNode').textContent = `${node.name} · ${id} · выбран ${counts.allocated}/${cohort.length} (${format(100 * counts.allocated / cohort.length)}%) · активен ${counts.active}/${cohort.length} · seeds: ${counts.seeds.join(', ') || '—'} · ${node.modifiers.map(m => m.name).join(', ') || node.type}`;
  }
  function drawHeat(fit) {
    heat.replaceChildren();
    const metric = $('heatMetric').value, visibleIds = new Set(heatNodes.keys());
    for (const id of heatNodes.keys()) for (const adjacent of byId.get(id)?.connectedIds || []) visibleIds.add(adjacent);
    const nodes = $('heatFull').checked ? catalog : catalog.filter(n => visibleIds.has(n.id));
    const visible = new Set(nodes.map(n => n.id)), edges = new Set();
    for (const node of nodes) for (const adjacent of node.connectedIds) {
      if (!visible.has(adjacent)) continue;
      const key = [node.id, adjacent].sort().join(':');
      if (edges.has(key)) continue;
      edges.add(key);
      const other = byId.get(adjacent);
      heat.append(svgElement('line', {x1: node.x, y1: -node.y, x2: other.x, y2: -other.y, stroke: '#354459', 'stroke-width': 0.05}));
    }
    for (const node of nodes) {
      const counts = heatNodes.get(node.id), frequency = (counts?.[metric] || 0) / cohort.length;
      const circle = svgElement('circle', {cx: node.x, cy: -node.y, r: node.root ? 0.25 : 0.18, fill: heatColor(frequency), stroke: '#101722', 'stroke-width': 0.03, cursor: 'pointer'});
      const title = svgElement('title', {});
      title.textContent = `${node.name}: ${counts?.[metric] || 0}/${cohort.length} (${format(frequency * 100)}%)`;
      circle.append(title); circle.onclick = () => inspectHeatNode(node.id); heat.append(circle);
      if (frequency > 0 && !node.root) {
        const label = svgElement('text', {x: node.x, y: -node.y + 0.045, 'text-anchor': 'middle', fill: frequency > 0.5 ? '#101722' : '#fff', 'font-size': 0.105, 'font-weight': 700, 'pointer-events': 'none'});
        label.textContent = Math.round(frequency * 100); heat.append(label);
      }
    }
    if (fit && nodes.length) {
      const xs = nodes.map(n => n.x), ys = nodes.map(n => -n.y);
      heatView = [Math.min(...xs) - 1, Math.min(...ys) - 1, Math.max(...xs) - Math.min(...xs) + 2, Math.max(...ys) - Math.min(...ys) + 2];
    }
    updateHeatView();
  }
  function drawMeanStats() {
    const query = $('meanStatFilter').value.toLowerCase();
    table($('meanStats'), meanRows.filter(row => row.stat.toLowerCase().includes(query) && ($('meanZero').checked || row.maximum !== 0)),
      [['stat', 'StatType'], ['average', 'Среднее'], ['minimum', 'Мин.'], ['maximum', 'Макс.'], ['samples', 'Ботов'], ['variants', 'Маска / варианты']]);
  }
  function selectStrategy() {
    cohort = groups.get($('strategy').value); heatNodes = new Map();
    $('strategyCount').textContent = `${cohort.length} запусков · seeds ${cohort.map(c => c.bot.seed).join(', ')}`;
    $('heatNode').textContent = 'Выберите узел.';
    for (const {bot, final} of cohort) {
      const allocated = new Set(bot.allocatedNodeIds || final.buildOrder || final.nodes.filter(n => !byId.get(n.id)?.root).map(n => n.id));
      for (const node of final.nodes) {
        if (!allocated.has(node.id) && !byId.get(node.id)?.root) continue;
        if (!heatNodes.has(node.id)) heatNodes.set(node.id, {allocated: 0, active: 0, seeds: []});
        const counts = heatNodes.get(node.id);
        counts.allocated++; counts.active += node.active ? 1 : 0; counts.seeds.push(bot.seed);
      }
    }
    const keys = [...new Set(cohort.flatMap(c => Object.keys(c.final.stats)))].sort();
    meanRows = keys.map(stat => {
      const values = cohort.map(c => c.final.stats[stat]).filter(Number.isFinite), mask = stat.endsWith('TypeMask');
      return {stat, average: mask ? null : mean(values), minimum: Math.min(...values), maximum: Math.max(...values), samples: `${values.length}/${cohort.length}`, variants: mask ? [...new Set(values)].join(', ') : ''};
    });
    const metrics = [
      ['Пройденный этап', c => c.bot.highestCompletedStage],
      ['Уровень', c => c.final.level],
      ['Пройденных локаций', c => c.bot.completedLocations.length],
      ['Смертей', c => c.bot.deaths], ['Волн', c => c.bot.waves], ['Убийств', c => c.bot.kills],
      ['Выбранных узлов', c => c.bot.allocatedNodeIds.length], ['Свободных очков', c => c.final.freePoints],
      ['Остаток золота', c => c.bot.gold],
      ['Куплено гемов', c => (c.bot.shopLog || []).filter(e => e.action === 'buy' && e.success).reduce((sum, e) => sum + (e.amount || 1), 0)],
      ['Гемов в сокетах', c => (c.final.socketGems || []).length],
      ['Боевого времени, минут', c => c.bot.simulatedSeconds / 60]
    ];
    table($('strategyResults'), metrics.map(([metric, read]) => {
      const values = cohort.map(read);
      return {metric, average: mean(values), minimum: Math.min(...values), maximum: Math.max(...values)};
    }), [['metric', 'Показатель'], ['average', 'Среднее на бота'], ['minimum', 'Мин.'], ['maximum', 'Макс.']]);
    table($('strategyRuns'), cohort.map(({bot, final}) => ({seed: bot.seed, stage: bot.highestCompletedStage, level: final.level, nodes: bot.allocatedNodeIds.length, gold: bot.gold, gems: final.socketGems?.length || 0, reason: bot.reason})),
      [['seed', 'Seed'], ['stage', 'Пройденный этап'], ['level', 'Уровень'], ['nodes', 'Узлов'], ['gold', 'Золото'], ['gems', 'Гемов'], ['reason', 'Причина остановки']]);
    drawMeanStats(); drawHeat(true);
  }
  for (const strategy of groups.keys()) option($('strategy'), strategy, strategy);
  $('strategyUI').hidden = !groups.size;
  $('strategy').onchange = selectStrategy;
  $('meanStatFilter').oninput = drawMeanStats; $('meanZero').onchange = drawMeanStats;
  $('heatMetric').onchange = () => drawHeat(false); $('heatFull').onchange = () => drawHeat(true);
  $('heatFit').onclick = () => drawHeat(true); $('heatZoomIn').onclick = () => heatZoom(0.75); $('heatZoomOut').onclick = () => heatZoom(1.33);
  heat.addEventListener('wheel', event => { event.preventDefault(); heatZoom(event.deltaY > 0 ? 1.15 : 0.87); }, {passive: false});
  heat.onpointerdown = event => { if (event.target.closest('circle')) return; heatDrag = [event.clientX, event.clientY, ...heatView]; heat.setPointerCapture(event.pointerId); };
  heat.onpointermove = event => {
    if (!heatDrag) return;
    const rect = heat.getBoundingClientRect(), scale = Math.max(heatDrag[4] / rect.width, heatDrag[5] / rect.height);
    heatView = [heatDrag[2] - (event.clientX - heatDrag[0]) * scale, heatDrag[3] - (event.clientY - heatDrag[1]) * scale, heatDrag[4], heatDrag[5]];
    updateHeatView();
  };
  heat.onpointerup = heat.onpointercancel = () => heatDrag = null;
  if (groups.size) selectStrategy();
}
