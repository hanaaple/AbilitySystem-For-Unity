// ─── Right panel metadata ─────────────────────────────────────
const SECTION_META = {
  overview: null,
  mvp: null,
  packages: {
    decisions: [
      'UniTask / R3로 async·반응형 패턴 통일 — 코루틴 혼용 없이 일관된 비동기 처리',
      '신 Input System 채택 — 키 리매핑·멀티 디바이스 대비, 레거시 완전 미사용',
      'AI Navigation으로 NavMesh 경로 — 직접 구현 대신 검증된 솔루션 채택',
    ],
    sources: ['Packages/manifest.json'],
  },
  arch: {
    decisions: [
      'Possession 패턴 — Controller · Character 분리로 AI 전환·멀티플레이 확장 대비',
      '이동 로직은 Character 소유 — 조작 주체(Controller)와 무관하게 동일 동작 보장',
      'HasAuthority() 현재 항상 true — 멀티 구현 시 권한 체계 추가 예정',
    ],
    sources: [
      'Scripts/Core/Controller.cs',
      'Scripts/Core/PlayerController.cs',
      'Scripts/PlayerCharacter.cs',
    ],
  },
  'ability-system': {
    decisions: [
      'GE 단일 채널 추상화 — 수십 개의 출처가 수치를 직접 조작하면 발생하는 순서 의존성·잔류 값 문제 제거',
      'dirty-on-write 전략 — CurrentValue는 Effect 변경 시점에만 재계산, 읽기 호출 빈도와 무관하게 비용 0',
      'SO 불변 정의 + 런타임 Spec 분리 — 여러 캐릭터가 SO 공유해도 적용 상태 간섭 없음',
      'Spec 생성 시 Reflection 1회 격리 — AttributeHandle이 FieldInfo 캐싱, 이후 런타임 string 탐색 없음',
    ],
    sources: ['Scripts/Core/AbilitySystem/'],
  },
  scenes: {
    decisions: [
      'ResultScene Additive 로드 — GameScene 데이터 유지하며 결과 오버레이 표시',
      'GameScene 세션마다 Single Load 리셋 — 상태 누적 없이 매 세션 클린 스타트 보장',
    ],
    sources: ['Scenes/'],
  },
  camera: {
    decisions: [
      'WorldSpace BindingMode — 캐릭터 회전과 카메라 방향 완전 독립, 탑다운 시야 고정',
      'CameraTarget 자식 오브젝트로 Follow 분리 — 카메라 오프셋을 코드 없이 Inspector 조정 가능',
    ],
    sources: ['Prefabs/Player.prefab'],
  },
  input: {
    decisions: [
      'E키 Tap / Hold 1s 분리 — 단일 키로 아이템 획득·채널링 두 인터랙션 처리',
      'PlayerInputActions.cs 자동 생성 — 소스는 .inputactions, 직접 수정 금지',
    ],
    sources: ['Input/PlayerInputActions.inputactions'],
  },
  convention: {
    decisions: [
      'Allman 중괄호 생략 금지 — 단일 라인 if 수정 시 발생하는 범위 오류 원천 차단',
      'private 필드 _ 접두사 — 지역 변수와 시각적으로 즉시 구분',
      '접근 한정자 항상 명시 — 의도하지 않은 internal 노출 방지',
    ],
    sources: ['.editorconfig'],
  },
  todo: null,
};

// ─── Meta card (floating) ─────────────────────────────────────
const metaCard = (() => {
  let el = null;

  return {
    init() {
      el = document.createElement('div');
      el.className = 'meta-card is-hidden';
      document.body.appendChild(el);
    },
    update(sectionId) {
      if (!el) return;
      const meta = SECTION_META[sectionId];
      if (!meta) { el.classList.add('is-hidden'); return; }

      const decisionsHtml = meta.decisions?.length
        ? `<div>
            <div class="meta-card__label">설계 결정</div>
            <ul class="meta-card__list">
              ${meta.decisions.map((d) => `<li>${d}</li>`).join('')}
            </ul>
          </div>`
        : '';

      const sourcesHtml = meta.sources?.length
        ? `<div>
            <div class="meta-card__label">소스</div>
            <ul class="meta-card__list meta-card__list--source">
              ${meta.sources.map((s) => `<li><code>${s}</code></li>`).join('')}
            </ul>
          </div>`
        : '';

      el.innerHTML = decisionsHtml + sourcesHtml;
      el.classList.toggle('is-hidden', !decisionsHtml && !sourcesHtml);
    },
  };
})();

// ─── Nav structure ────────────────────────────────────────────
const NAV = [
  {
    label: '개요',
    links: [
      { href: '#overview', icon: '🎮', text: '게임 개요' },
      { href: '#mvp',      icon: '📋', text: 'MVP 현황' },
      { href: '#packages', icon: '📦', text: '패키지' },
    ],
  },
  {
    label: '아키텍처',
    links: [
      { href: '#arch',           icon: '🏗', text: '컨트롤러·캐릭터' },
      { href: '#ability-system', icon: '⚡', text: 'AbilitySystem', children: [
        { href: '#attr-system', text: 'Attribute' },
        { href: '#ge-system',   text: 'GameplayEffect' },
      ]},
      { href: '#scenes', icon: '🗺', text: '씬 구성' },
      { href: '#camera', icon: '🎥', text: '카메라' },
      { href: '#input',  icon: '⌨',  text: '입력 액션맵' },
    ],
  },
  {
    label: '개발 가이드',
    links: [
      { href: '#convention', icon: '📐', text: '코드 컨벤션', children: [
        { href: '#convention-naming', text: '네이밍 규칙' },
        { href: '#convention-style',  text: '스타일 규칙' },
      ]},
      { href: '#todo', icon: '✅', text: 'TODO', children: [
        { href: '#todo-github',  text: 'GitHub 설정' },
        { href: '#todo-ability', text: 'AbilitySystem' },
        { href: '#todo-item',    text: 'Item Module · Equipment' },
      ]},
    ],
  },
];

// ─── Render sidebar nav ───────────────────────────────────────
function initNav() {
  const container = document.getElementById('sidebar-nav');
  if (!container) return;

  const fragment = document.createDocumentFragment();

  for (const section of NAV) {
    const label = document.createElement('div');
    label.className = 'sidebar__section-label';
    label.textContent = section.label;
    fragment.appendChild(label);

    for (const link of section.links) {
      const a = document.createElement('a');
      a.href = link.href;
      a.className = 'sidebar__link';
      a.innerHTML = `<span class="icon">${link.icon}</span> ${link.text}`;
      fragment.appendChild(a);

      if (link.children) {
        for (const child of link.children) {
          const sub = document.createElement('a');
          sub.href = child.href;
          sub.className = 'sidebar__sublink';
          sub.textContent = child.text;
          fragment.appendChild(sub);
        }
      }
    }
  }

  container.appendChild(fragment);
}

// ─── Tabs ─────────────────────────────────────────────────────
function activateTab(bar, tabId) {
  bar.querySelectorAll('.tab-btn').forEach((btn) => {
    btn.classList.toggle('is-active', btn.dataset.tab === tabId);
  });
  document.querySelectorAll(`.tab-panel[data-tabs="${bar.id}"]`).forEach((panel) => {
    const becoming = panel.dataset.panel === tabId;
    panel.classList.toggle('is-hidden', !becoming);
    if (becoming) {
      const accordions = panel.querySelectorAll('details.accordion');
      accordions.forEach((d, i) => { d.open = i === 0; });
      const section = bar.closest('.section');
      if (section) dotNav.update(section.id);
    }
  });
}

function initTabs() {
  document.querySelectorAll('.tab-bar').forEach((bar) => {
    bar.querySelectorAll('.tab-btn').forEach((btn) => {
      btn.addEventListener('click', () => activateTab(bar, btn.dataset.tab));
    });
  });
}

// ─── SPA Navigation ───────────────────────────────────────────
function getSectionForId(id) {
  const el = document.getElementById(id);
  if (!el) return null;
  if (el.classList.contains('section')) return id;
  const parent = el.closest('.section');
  return parent ? parent.id : null;
}

function setNavActive(sectionId, subId) {
  document.querySelectorAll('.sidebar__link, .sidebar__sublink').forEach((link) => {
    const id = link.getAttribute('href').slice(1);
    const active = subId ? id === subId : id === sectionId;
    link.classList.toggle('is-active', active);
  });
}

function handleSubNav(id) {
  const el = document.getElementById(id);
  if (!el) return;

  const panel = el.closest('.tab-panel[data-tabs]');
  if (panel?.classList.contains('is-hidden')) {
    const bar = document.getElementById(panel.dataset.tabs);
    if (bar) activateTab(bar, panel.dataset.panel);
  }

  const accordion = el.closest('details.accordion');
  if (accordion && !accordion.open) accordion.open = true;
}

function navigateTo(sectionId, subId = null) {
  // 모든 섹션 숨김
  document.querySelectorAll('.section').forEach((s) => s.classList.remove('is-active'));

  // 히어로는 overview일 때만 표시
  const hero = document.querySelector('.hero');
  if (hero) hero.classList.toggle('is-active', sectionId === 'overview');

  // 대상 섹션 표시
  const section = document.getElementById(sectionId);
  if (section) section.classList.add('is-active');

  // 메인 스크롤 최상단
  window.scrollTo(0, 0);

  // URL 업데이트
  const hash = subId ? `#${subId}` : `#${sectionId}`;
  history.pushState(null, '', hash);

  // 사이드바 활성 상태
  setNavActive(sectionId, subId);

  // floating 카드들
  metaCard.update(sectionId);
  dotNav.update(sectionId);

  // 탭·아코디언 하위 이동
  if (subId) requestAnimationFrame(() => handleSubNav(subId));
}

function resolveAndNavigate(id) {
  const sectionId = getSectionForId(id);
  if (!sectionId) return;
  const subId = id !== sectionId ? id : null;
  navigateTo(sectionId, subId);
}

function initNavigation() {
  // 모든 앵커 클릭 가로채기
  document.addEventListener('click', (e) => {
    const a = e.target.closest('a[href^="#"]');
    if (!a) return;
    const id = a.getAttribute('href').slice(1);
    if (!id) return;
    e.preventDefault();
    resolveAndNavigate(id);
  });

  // 뒤로/앞으로
  window.addEventListener('popstate', () => {
    const id = location.hash.slice(1) || 'overview';
    resolveAndNavigate(id);
  });

  // 초기 진입
  const initialId = location.hash.slice(1) || 'overview';
  resolveAndNavigate(initialId);
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

// ─── Todo checkboxes ──────────────────────────────────────────
function initTodoCheckboxes() {
  const STORAGE_KEY = 'spectral-raid-todo';
  const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}');

  function save(id, checked) {
    saved[id] = checked;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(saved));
  }

  function setChecked(checkbox, checked) {
    const li = checkbox.closest('li');
    checkbox.classList.toggle('is-checked', checked);
    if (li) li.classList.toggle('is-done', checked);
  }

  function updateGroupProgress(group) {
    const checkboxes = group.querySelectorAll('.todo-list__checkbox');
    const total = checkboxes.length;
    if (total === 0) return;
    const done = group.querySelectorAll('.todo-list__checkbox.is-checked').length;
    const pct = Math.round((done / total) * 100);

    const progressEl = group.querySelector('.todo-group__progress');
    if (progressEl) progressEl.textContent = `${done} / ${total}`;

    const barFill = group.querySelector('.todo-group__bar-fill');
    if (barFill) barFill.style.width = `${pct}%`;
  }

  document.querySelectorAll('.todo-list__checkbox').forEach((checkbox) => {
    const id = checkbox.dataset.id;
    if (!id) return;

    if (saved[id]) setChecked(checkbox, true);

    checkbox.addEventListener('click', () => {
      const next = !checkbox.classList.contains('is-checked');
      setChecked(checkbox, next);
      save(id, next);
      const group = checkbox.closest('.todo-group');
      if (group) updateGroupProgress(group);
    });
  });

  document.querySelectorAll('.todo-group').forEach(updateGroupProgress);
}

// ─── Dot nav ──────────────────────────────────────────────────
const dotNav = (() => {
  let el = null;
  let current = [];

  function getAccordions(sectionId) {
    const section = document.getElementById(sectionId);
    if (!section) return [];
    const panel = section.querySelector('.tab-panel:not(.is-hidden)');
    return Array.from((panel ?? section).querySelectorAll(':scope > details.accordion'));
  }

  function sync() {
    el?.querySelectorAll('.dot-nav__dot').forEach((dot, i) => {
      dot.classList.toggle('is-active', current[i]?.open ?? false);
    });
  }

  function render(accordions) {
    current = accordions;
    if (!el) return;

    if (accordions.length < 2) {
      el.classList.add('is-hidden');
      return;
    }

    el.classList.remove('is-hidden');
    el.innerHTML = accordions.map((acc, i) =>
      `<div class="dot-nav__dot${acc.open ? ' is-active' : ''}" data-i="${i}"></div>`
    ).join('');

    el.querySelectorAll('.dot-nav__dot').forEach((dot) => {
      dot.addEventListener('click', () => {
        const i = +dot.dataset.i;
        current.forEach((a, j) => { a.open = j === i; });
        sync();
      });
    });
  }

  return {
    init() {
      el = document.createElement('div');
      el.className = 'dot-nav';
      document.body.appendChild(el);
      document.addEventListener('toggle', (e) => {
        if (e.target.matches('details.accordion')) sync();
      }, true);
    },
    update(sectionId) { render(getAccordions(sectionId)); },
  };
})();

// ─── Accordion exclusive open ─────────────────────────────────
function initAccordionExclusive() {
  document.addEventListener('toggle', (e) => {
    const details = e.target;
    if (!details.matches('details.accordion') || !details.open) return;

    const parent = details.parentElement;
    parent.querySelectorAll('details.accordion').forEach((sibling) => {
      if (sibling !== details) sibling.open = false;
    });
  }, true);
}

// ─── Boot ─────────────────────────────────────────────────────
if ('scrollRestoration' in history) history.scrollRestoration = 'manual';

document.addEventListener('DOMContentLoaded', () => {
  initNav();
  initTabs();
  metaCard.init();
  dotNav.init();
  initNavigation();
  initAccordionExclusive();
  initCodeCopy();
  initTodoCheckboxes();
});
