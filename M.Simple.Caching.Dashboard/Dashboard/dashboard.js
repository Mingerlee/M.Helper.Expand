'use strict';
const $ = id => document.getElementById(id);
const base = new URL(location.pathname.endsWith('/') ? location.pathname : location.pathname + '/', location.origin);
let currentPrefix = '', cursors = [null], pageIndex = 0, nextCursor = null, readOnly = true, plan = null, busy = false, scope = 'CurrentApplication';
function message(text, error = false) { $('message').textContent = text; $('message').classList.toggle('error', error); }
function time(value) { return value ? new Date(value).toLocaleString() : '不主动过期'; }
async function api(path, body) {
    const options = { credentials: 'same-origin', cache: 'no-store' };
    if (body !== undefined) {
        const tokenResponse = await fetch(new URL('api/token', base), options);
        if (!tokenResponse.ok) throw new Error('无法获取操作令牌，请确认登录状态。');
        const token = await tokenResponse.json();
        options.method = 'POST';
        options.headers = { 'Content-Type': 'application/json', [token.header]: token.token };
        options.body = JSON.stringify(body);
    }
    const response = await fetch(new URL('api/' + path, base), options);
    if (!response.ok) {
        if (response.status === 404) throw new Error('缓存已过期或已删除。');
        if (response.status === 401 || response.status === 403) throw new Error('没有权限或登录已失效。');
        let reason = '操作失败，请稍后重试。';
        try { const data = await response.json(); reason = data.error || data.detail || reason; } catch {}
        throw new Error(reason);
    }
    return response.json();
}
function cell(row, text) { const td = row.insertCell(); td.textContent = String(text); return td; }
function button(text, action, danger = false) {
    const node = document.createElement('button'); node.textContent = text; node.className = danger ? 'danger write' : 'secondary';
    node.onclick = () => run(action); return node;
}
async function run(action) {
    if (busy) return;
    busy = true;
    try { await action(); } catch (error) { message(error.message, true); }
    finally { busy = false; }
}
async function status() {
    const data = await api('status');
    readOnly = data.readOnly;
    document.body.classList.toggle('read-only', readOnly);
    $('backend').textContent = data.backend;
    scope = data.scope;
    $('scope-mode').textContent = scope === 'All' ? '全部数据' : scope === 'AllManaged' ? '所有应用的本包缓存' : scope === 'Prefixes' ? '指定前缀（全部数据）' : '当前应用';
    $('scope').textContent = scope === 'All' || scope === 'AllManaged' ? '当前后端存储范围' : data.prefixes.join('、');
    $('prefix').placeholder = scope === 'CurrentApplication' ? '例如 product:' : '完整 Key 前缀，例如 Bams:Excel:';
    $('instance').textContent = '当前实例 ' + data.instance;
    $('mode').textContent = readOnly ? '只读模式' : '管理模式';
    const s = data.statistics, reads = s.hits + s.misses;
    $('ratio').textContent = reads ? (s.hits / reads * 100).toFixed(1) + '%' : '—';
    $('reads').textContent = s.hits + ' / ' + s.misses;
    $('loads').textContent = s.loads; $('failures').textContent = s.loadFailures;
}
async function load() {
    const query = new URLSearchParams({ prefix: currentPrefix, pageSize: '50' });
    if (cursors[pageIndex]) query.set('cursor', cursors[pageIndex]);
    const data = await api('entries?' + query);
    nextCursor = data.nextCursor;
    const tbody = $('entries'); tbody.replaceChildren(); $('select-all').checked = false;
    for (const item of data.items) {
        const row = tbody.insertRow();
        const selection = row.insertCell(); selection.className = 'write';
        const check = document.createElement('input'); check.type = 'checkbox'; check.dataset.key = item.key; check.setAttribute('aria-label', '选择 ' + item.key); selection.append(check);
        cell(row, item.key); cell(row, item.typeName); cell(row, item.sizeBytes >= 0 ? item.sizeBytes + ' B' : item.itemCount != null ? item.itemCount + ' 项' : '—'); cell(row, time(item.expiresAt));
        const actions = row.insertCell();
        actions.append(button('详情', () => detail(item.key)), button('删除', () => remove([item.key]), true));
    }
    if (!data.items.length) { const row = tbody.insertRow(); const td = cell(row, nextCursor ? '本页暂无有效项，可继续下一页。' : '暂无匹配缓存。'); td.colSpan = 6; }
    $('page-label').textContent = '第 ' + (pageIndex + 1) + ' 页 · ' + data.items.length + ' 项';
    $('previous').disabled = pageIndex === 0; $('next').disabled = !nextCursor;
}
async function detail(key) {
    const data = await api('entry?' + new URLSearchParams({ key }));
    const e = data.entry;
    $('metadata').textContent = 'Key：' + e.key + '\n类型：' + e.typeName + '\n大小：' + (e.sizeBytes >= 0 ? e.sizeBytes + ' B' : e.itemCount != null ? e.itemCount + ' 项' : '—') + '\n当前过期：' + time(e.expiresAt) + '\n绝对上限：' + time(e.absoluteExpiresAt) + '\n滑动期限：' + (e.slidingExpiration || '未设置');
    try { $('json').textContent = JSON.stringify(JSON.parse(data.json), null, 2); } catch { $('json').textContent = data.json; }
    $('detail').showModal();
}
async function audit() {
    const data = await api('operations'); $('operations').replaceChildren();
    for (const op of data) { const row = $('operations').insertRow(); [time(op.at), op.actor, op.action, op.key, op.outcome].forEach(v => cell(row, v)); }
}
async function reset() { pageIndex = 0; cursors = [null]; await load(); }
async function remove(keys) {
    if (readOnly || !keys.length) { message('请先选择缓存。'); return; }
    if (!confirm('确认删除 ' + keys.length + ' 个缓存 Key？')) return;
    const result = await api('remove', { keys });
    await reset(); await audit(); message('已删除 ' + result.removed + ' 项，其他项已不存在。');
}
$('search').onsubmit = event => { event.preventDefault(); run(async () => { currentPrefix = $('prefix').value; await reset(); message(''); }); };
$('refresh').onclick = () => run(async () => { await status(); await reset(); message('已刷新。'); });
$('previous').onclick = () => run(async () => { pageIndex--; await load(); });
$('next').onclick = () => run(async () => { cursors[++pageIndex] = nextCursor; await load(); });
$('select-all').onchange = () => document.querySelectorAll('#entries input[type=checkbox]').forEach(c => c.checked = $('select-all').checked);
$('batch').onclick = () => run(() => remove([...document.querySelectorAll('#entries input:checked')].map(c => c.dataset.key)));
$('preview').onclick = () => run(async () => {
    if (readOnly) return;
    const prefix = $('prefix').value;
    if ((scope === 'All' || scope === 'AllManaged') && !prefix.trim()) { message('管理全部数据或所有应用缓存时，请输入非空的完整 Key 前缀后再清理。', true); return; }
    const data = await api('preview', { prefix }); plan = data.token;
    $('preview-description').textContent = (scope === 'CurrentApplication' ? '相对前缀' : '完整 Key 前缀') + '“' + prefix + '”匹配 ' + data.keys.length + ' 个 Key。' + (data.partial ? '本次为部分结果，后续可再次预览清理。' : '') + ' 预览在两分钟内有效。';
    $('preview-keys').textContent = data.keys.join('\n') || '暂无匹配项';
    $('confirm-clear').disabled = !data.keys.length; $('clear-dialog').showModal();
});
$('confirm-clear').onclick = () => run(async () => {
    const token = plan; plan = null; $('clear-dialog').close();
    const result = await api('clear', { token }); await reset(); await audit(); message('本次清理完成，已删除 ' + result.removed + ' 项。');
});
$('cancel-clear').onclick = () => { plan = null; $('clear-dialog').close(); };
$('close-detail').onclick = () => $('detail').close();
$('audit-refresh').onclick = () => run(audit);
run(async () => { await status(); await load(); await audit(); });
setInterval(() => { if (!busy) status().catch(error => message(error.message, true)); }, 15000);
