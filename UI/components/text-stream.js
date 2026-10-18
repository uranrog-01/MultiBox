/**
 * ObsidianUI TextStream Component
 * Direct port of https://www.obsidianui.dev/r/text-stream.json for native browser & WebView2
 */
(function() {
  'use strict';

  class TextStreamComponent {
    constructor(element, options = {}) {
      this.container = typeof element === 'string' ? document.getElementById(element) : element;
      if (!this.container) return;

      this.items = options.items || [
        'MultiBox Sandbox',
        'zer0ne (Z1)',
        'Zero-Trace Ephemeral Vaults',
        'v1.0.4 Production',
        'Live Profile Snapshot',
        'github.com/uranrog-01/MultiBox'
      ];
      this.prefix = options.prefix || 'ARCHITECTURE • SYSTEM STREAM';
      this.baseSpeed = options.speed || -0.6;
      this.maxBoost = 12;
      this.paused = false;

      this.currentY = 0;
      this.distance = 0;
      this.currentVelocity = this.baseSpeed;
      this.targetVelocity = this.baseSpeed;
      this.lastScrollDirection = -1;
      this.scrollTimeout = null;
      this.animId = null;

      this.render();
      this.initAnimation();
    }

    render() {
      this.container.classList.add('obsidian-text-stream');
      this.container.innerHTML = `
        <p class="obsidian-text-stream__prefix">${this.prefix}</p>
        <div class="obsidian-text-stream__viewport">
          <div class="obsidian-text-stream__track">
            <div class="obsidian-text-stream__copy">
              ${this.items.map(text => `<div class="obsidian-text-stream__item">${text}</div>`).join('')}
            </div>
          </div>
        </div>
      `;

      this.viewport = this.container.querySelector('.obsidian-text-stream__viewport');
      this.track = this.container.querySelector('.obsidian-text-stream__track');
      this.content = this.container.querySelector('.obsidian-text-stream__copy');

      // Duplicate copies for infinite seamless wrap
      const secondCopy = this.content.cloneNode(true);
      secondCopy.setAttribute('aria-hidden', 'true');
      this.track.appendChild(secondCopy);
      const thirdCopy = this.content.cloneNode(true);
      thirdCopy.setAttribute('aria-hidden', 'true');
      this.track.appendChild(thirdCopy);
    }

    initAnimation() {
      if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

      const updateMetrics = () => {
        if (!this.content || !this.viewport) return;
        this.distance = this.content.offsetHeight;
        if (!this.distance) return;

        // Wrap current position within distance
        while (this.currentY <= -this.distance) this.currentY += this.distance;
        while (this.currentY > 0) this.currentY -= this.distance;
      };

      updateMetrics();
      window.addEventListener('resize', updateMetrics);

      let lastTime = performance.now();
      const tick = (now) => {
        if (!this.paused && this.distance > 0) {
          const deltaTime = Math.min(now - lastTime, 50);
          lastTime = now;
          const frameFactor = deltaTime / 16.666;

          // Smooth velocity interpolation (lerp factor 0.14 from ObsidianUI spec)
          this.currentVelocity += (this.targetVelocity - this.currentVelocity) * 0.14;
          this.currentY += this.currentVelocity * frameFactor;

          // Wrap seamlessly
          while (this.currentY <= -this.distance) this.currentY += this.distance;
          while (this.currentY > 0) this.currentY -= this.distance;

          this.track.style.transform = `translate3d(0, ${this.currentY}px, 0)`;
        } else {
          lastTime = now;
        }
        this.animId = requestAnimationFrame(tick);
      };

      this.animId = requestAnimationFrame(tick);

      const applyScrollMotion = (delta) => {
        if (!delta) return;
        const direction = delta > 0 ? -1 : 1;
        const boost = Math.min(this.maxBoost, Math.abs(this.baseSpeed) + Math.pow(Math.abs(delta), 1.2) * 0.08);
        this.lastScrollDirection = direction;
        this.targetVelocity = direction * boost;
        clearTimeout(this.scrollTimeout);
        this.scrollTimeout = setTimeout(() => {
          this.targetVelocity = this.lastScrollDirection * Math.abs(this.baseSpeed);
        }, 120);
      };

      this.viewport.addEventListener('wheel', (e) => applyScrollMotion(e.deltaY), { passive: true });
    }

    destroy() {
      if (this.animId) cancelAnimationFrame(this.animId);
      clearTimeout(this.scrollTimeout);
    }
  }

  window.ObsidianTextStream = TextStreamComponent;
})();

