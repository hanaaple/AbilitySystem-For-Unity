// ─── Nav active state via IntersectionObserver ────────────────
function initNavHighlight() {
  const links = Array.from(document.querySelectorAll('.sidebar__link[href^="#"]'));

  const entries = links.flatMap((link) => {
    const href = link.getAttribute('href');
    if (!href) return [];
    const id = href.slice(1);
    const target = document.getElementById(id);
    if (!target) return [];
    return [{ id, link }];
  });

  if (entries.length === 0) return;

  const setActive = (id) => {
    entries.forEach(({ link, id: entryId }) => {
      link.classList.toggle('is-active', entryId === id);
    });
  };

  const observer = new IntersectionObserver(
    (observedEntries) => {
      const visible = observedEntries.find((e) => e.isIntersecting);
      if (visible) setActive(visible.target.id);
    },
    { rootMargin: '-15% 0px -70% 0px' }
  );

  entries.forEach(({ id }) => {
    const el = document.getElementById(id);
    if (el) observer.observe(el);
  });

  if (entries[0]) setActive(entries[0].id);
}

// ─── Smooth scroll for anchor links ───────────────────────────
function initSmoothScroll() {
  document.querySelectorAll('a[href^="#"]').forEach((a) => {
    a.addEventListener('click', (e) => {
      const href = a.getAttribute('href');
      if (!href) return;
      const target = document.querySelector(href);
      if (!target) return;
      e.preventDefault();
      target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  });
}

// ─── Copy button on code blocks ───────────────────────────────
function initCodeCopy() {
  document.querySelectorAll('.code-block').forEach((block) => {
    const header = block.querySelector('.code-block__header');
    const pre = block.querySelector('pre');
    if (!header || !pre) return;

    const btn = document.createElement('button');
    btn.className = 'copy-btn';
    btn.textContent = 'copy';
    header.appendChild(btn);

    btn.addEventListener('click', () => {
      navigator.clipboard.writeText(pre.innerText).then(() => {
        btn.textContent = 'copied!';
        btn.classList.add('is-copied');
        setTimeout(() => {
          btn.textContent = 'copy';
          btn.classList.remove('is-copied');
        }, 1800);
      });
    });
  });
}

// ─── Boot ─────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  initNavHighlight();
  initSmoothScroll();
  initCodeCopy();
});
