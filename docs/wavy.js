(function () {
  var reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  var canvas = document.createElement("canvas");
  canvas.className = "app-wavy";
  canvas.setAttribute("aria-hidden", "true");
  var css = document.createElement("style");
  css.textContent = "html{background:#08090d}body{background:transparent}.app-wavy{position:fixed;inset:0;width:100%;height:100%;z-index:-1;pointer-events:none}";
  document.head.appendChild(css);
  document.body.prepend(canvas);

  var ctx = canvas.getContext("2d");
  var start = performance.now();
  var running = false;

  function resize() {
    var dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.max(1, Math.floor(window.innerWidth * dpr));
    canvas.height = Math.max(1, Math.floor(window.innerHeight * dpr));
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  function draw(now) {
    var w = window.innerWidth;
    var h = window.innerHeight;
    var t = reduce ? 0 : (now - start) / 1000;
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = "#08090d";
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = "rgba(107, 159, 255, 0.0706)";
    for (var wave = 0; wave < 4; wave++) {
      ctx.beginPath();
      ctx.moveTo(0, h);
      for (var x = 0; x <= w; x += 8) {
        var y = h * (0.35 + wave * 0.12) + Math.sin(x * 0.01 + t * 1.2 + wave) * 30;
        ctx.lineTo(x, y);
      }
      ctx.lineTo(w, h);
      ctx.closePath();
      ctx.fill();
    }
  }

  function loop(now) {
    if (document.hidden) {
      running = false;
      return;
    }
    draw(now);
    requestAnimationFrame(loop);
  }

  function startLoop() {
    if (running) return;
    running = true;
    requestAnimationFrame(loop);
  }

  resize();
  window.addEventListener("resize", function () {
    resize();
    if (reduce) draw(0);
  });

  if (reduce) {
    draw(0);
    return;
  }

  startLoop();
  document.addEventListener("visibilitychange", function () {
    if (!document.hidden) startLoop();
  });
})();
