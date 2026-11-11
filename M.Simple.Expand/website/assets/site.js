(function () {
  'use strict';
  const root = document.body.dataset.root || '';
  const dialog = document.querySelector('.search-dialog');
  const input = document.querySelector('#search-input');
  const results = document.querySelector('.search-results');
  const index = window.MSIMPLE_SEARCH || [];
  const showSearch = function () { dialog.showModal(); input.focus(); renderResults(); };
  document.querySelector('.search-trigger').addEventListener('click', showSearch);
  document.querySelector('.close-search').addEventListener('click', function () { dialog.close(); });
  dialog.addEventListener('click', function (event) { if (event.target === dialog) dialog.close(); });
  document.addEventListener('keydown', function (event) {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); if (!dialog.open) showSearch(); }
    if (event.key === 'Escape') closeNavigation();
  });
  function renderResults() {
    const query = input.value.trim().toLowerCase();
    results.replaceChildren();
    if (!query) { const p = document.createElement('p'); p.className = 'search-empty'; p.textContent = '输入方法名或关键字，例如 MaskPhone、ToTree、Compare。'; results.appendChild(p); return; }
    const words = query.split(/\s+/);
    const matches = index.map(function (page) {
      const content = (page.title + ' ' + page.label + ' ' + page.text).toLowerCase();
      return { page: page, score: words.every(function (word) { return content.includes(word); }) ? 1 + words.reduce(function (score, word) { return score + (page.title.toLowerCase().includes(word) ? 10 : 0) + (page.label.toLowerCase().includes(word) ? 5 : 0); }, 0) : 0 };
    }).filter(function (item) { return item.score > 0; }).sort(function (a, b) { return b.score - a.score; }).slice(0, 12);
    if (!matches.length) { const p = document.createElement('p'); p.className = 'search-empty'; p.textContent = '没有匹配内容，请尝试其他方法名或关键字。'; results.appendChild(p); }
    matches.forEach(function (item) {
      const link = document.createElement('a'); link.className = 'search-result'; link.href = root + item.page.url;
      const title = document.createElement('strong'); title.textContent = item.page.title;
      const label = document.createElement('small'); label.textContent = item.page.label;
      const excerpt = document.createElement('p');
      const position = item.page.text.toLowerCase().indexOf(words[0]);
      excerpt.textContent = (position > 35 ? '…' : '') + item.page.text.slice(Math.max(0, position - 35), Math.max(0, position - 35) + 130) + '…';
      link.append(title, label, excerpt); results.appendChild(link);
    });
  }
  input.addEventListener('input', renderResults);
  input.addEventListener('keydown', function (event) { if (event.key === 'ArrowDown') { const first = results.querySelector('a'); if (first) { event.preventDefault(); first.focus(); } } });
  const menu = document.querySelector('.menu-button'), backdrop = document.querySelector('.nav-backdrop');
  function closeNavigation() { document.body.classList.remove('nav-open'); backdrop.hidden = true; menu.setAttribute('aria-expanded', 'false'); }
  menu.addEventListener('click', function () { const opening = !document.body.classList.contains('nav-open'); document.body.classList.toggle('nav-open', opening); backdrop.hidden = !opening; menu.setAttribute('aria-expanded', String(opening)); });
  backdrop.addEventListener('click', closeNavigation);
  let toastTimer;
  function toast(message) { const element = document.querySelector('.toast'); element.textContent = message; element.classList.add('visible'); clearTimeout(toastTimer); toastTimer = setTimeout(function () { element.classList.remove('visible'); }, 1800); }
  document.querySelectorAll('.copy-button').forEach(function (button) {
    button.addEventListener('click', async function () {
      const text = button.closest('.code-block').querySelector('pre code').textContent;
      try {
        if (navigator.clipboard && window.isSecureContext) await navigator.clipboard.writeText(text);
        else { const area = document.createElement('textarea'); area.value = text; area.style.position = 'fixed'; area.style.left = '-9999px'; document.body.appendChild(area); area.select(); const copied = document.execCommand('copy'); area.remove(); if (!copied) throw new Error('copy'); }
        toast('代码已复制');
      } catch (_) { toast('复制未成功，请选中代码手动复制'); }
    });
  });
  document.querySelectorAll('code[data-language="csharp"]').forEach(function (code) {
    const source = code.textContent, fragment = document.createDocumentFragment(); let position = 0;
    const tokens = /\/\/[^\n]*|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|\b(?:using|namespace|public|private|internal|static|sealed|class|interface|enum|var|new|return|await|async|void|bool|string|int|long|decimal|true|false|null|if|else|foreach|in|throw|const|get|set)\b|\b\d+(?:\.\d+)?m?\b/g;
    let match;
    while ((match = tokens.exec(source))) {
      fragment.appendChild(document.createTextNode(source.slice(position, match.index)));
      const span = document.createElement('span'); span.textContent = match[0]; span.className = match[0].startsWith('//') ? 'token-comment' : /^["']/.test(match[0]) ? 'token-string' : /^\d/.test(match[0]) ? 'token-number' : 'token-keyword'; fragment.appendChild(span); position = tokens.lastIndex;
    }
    fragment.appendChild(document.createTextNode(source.slice(position))); code.replaceChildren(fragment);
  });
  if ('IntersectionObserver' in window) {
    const links = Array.from(document.querySelectorAll('.page-toc a'));
    const observer = new IntersectionObserver(function (entries) { entries.forEach(function (entry) { if (entry.isIntersecting) { links.forEach(function (link) { link.classList.toggle('current', link.hash === '#' + entry.target.id); }); } }); }, { rootMargin: '-80px 0px -65% 0px' });
    document.querySelectorAll('h2[id],h3[id],section[id]').forEach(function (heading) { observer.observe(heading); });
  }
})();
