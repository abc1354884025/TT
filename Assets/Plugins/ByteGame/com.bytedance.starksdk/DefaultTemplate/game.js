const unityNamespace = require("./unity-namespace.js");
const wasmSplitValues = require('./webgl-wasm-split');
const {
  launchEventType,
  scaleMode
} = require('./plugin-config.js');
require('./webgl.framework.js');
require('./plugin-config.js');

const managerConfig = {
     DATA_CDN: "$DEPLOY_URL",
     DATA_FILE_MD5: "$DATA_MD5",
     CODE_FILE_MD5: "$CODE_MD5",
     GAME_NAME: "$GAME_NAME",
     APPID: "$APP_ID",
     DATA_FILE_SIZE: "$DATA_FILE_SIZE",
     OPT_DATA_FILE_SIZE: "$OPT_DATA_FILE_SIZE",
     useDataCDNAsStreamingAssetsUrl: $USE_DATA_CDN,

     loadDataPackageFromSubpackage: $LOAD_DATA_FROM_SUBPACKAGE,
     compressDataPackage: $COMPRESS_DATA_PACKAGE,
	   ...wasmSplitValues,


     preloadDataList: [
        $PRELOAD_LIST,
    ],

    cpJsFiles: [
      $CPJSFILES
    ],

     urlCacheList: [
		$URL_CACHE_LIST
     ],
     dontCacheFileNames: [
		$DONT_CACHE_FILE_NAMES
     ]
};
GameGlobal.managerConfig = managerConfig;

// ============ 自定义加载界面 ============

// 加载阶段 → 进度映射
var loadStages = [
  { type: launchEventType.launchPlugin,  text: '初始化引擎...',    progress: 0.05 },
  { type: launchEventType.loadWasm,      text: '下载程序包...',    progress: 0.30 },
  { type: launchEventType.compileWasm,   text: '编译代码...',      progress: 0.60 },
  { type: launchEventType.loadAssets,    text: '加载资源...',      progress: 0.80 },
  { type: launchEventType.readAssets,    text: '读取资源...',      progress: 0.90 },
  { type: launchEventType.prepareGame,   text: '准备就绪',         progress: 1.00 },
];

var currentProgress = 0;
var currentText = '启动中...';
var isComplete = false;

// 绘制函数
function drawLoading(ctx, w, h, progress, text) {
  // 清屏
  ctx.fillStyle = '#1a1a2e';
  ctx.fillRect(0, 0, w, h);

  // 标题
  ctx.fillStyle = '#ffffff';
  ctx.font = 'bold 24px sans-serif';
  ctx.textAlign = 'center';
  ctx.fillText('背包乱斗', w / 2, h * 0.35);

  // 小提示
  ctx.font = '14px sans-serif';
  ctx.fillStyle = '#888888';
  ctx.fillText('首次加载请耐心等待', w / 2, h * 0.42);

  // 进度条背景
  var barW = w * 0.65;
  var barH = 8;
  var barX = (w - barW) / 2;
  var barY = h * 0.52;

  ctx.fillStyle = '#333355';
  ctx.fillRect(barX, barY, barW, barH);

  // 进度条填充
  var p = Math.max(0, Math.min(1, progress));
  ctx.fillStyle = '#ff6600';
  ctx.fillRect(barX, barY, barW * p, barH);

  // 百分比文字
  ctx.fillStyle = '#ffffff';
  ctx.font = '16px sans-serif';
  ctx.fillText(Math.floor(p * 100) + '%', w / 2, barY + barH + 28);

  // 状态文字
  ctx.fillStyle = '#cccccc';
  ctx.font = '13px sans-serif';
  ctx.fillText(text, w / 2, barY + barH + 50);

  // 底部版权
  ctx.fillStyle = '#555555';
  ctx.font = '11px sans-serif';
  ctx.fillText('Powered by Unity + HybridCLR', w / 2, h * 0.85);
}

// 循环刷新
function renderLoop(ctx, w, h) {
  drawLoading(ctx, w, h, currentProgress, currentText);
  if (!isComplete) {
    requestAnimationFrame(function() { renderLoop(ctx, w, h); });
  }
}

// ============ 主函数 ============

function main() {
  const UnityManager = requirePlugin('UnityPlugin/index.js');
  console.log("UnityManager.version = ", UnityManager.version);
  const info = tt.getSystemInfoSync();
  const canvas = tt.createCanvas();
  canvas.width = info.screenWidth;
  canvas.height = info.screenHeight;

  // 获取 Canvas 2D 上下文用于自定义加载 UI
  var ctx = canvas.getContext('2d');
  var cw = info.screenWidth;
  var ch = info.screenHeight;

  Object.assign(managerConfig, {
    hideAfterCallmain: $HIDE_AFTER_CALLMAIN,

    // ★ 禁用默认加载页，用自定义 Canvas 绘制
    disableLoadingPage: true,
    loadingPageConfig: {
      designWidth: 0,
      designHeight: 0,
      scaleMode: scaleMode.default,
      textConfig: {
        firstStartText: '首次加载请耐心等待',
        downloadingText: ['正在加载资源'],
        compilingText: '编译中',
        initText: '初始化中',
        completeText: '开始游戏',
        textDuration: 1500,
        style: {
          bottom: $TEXTCONFIG_BOTTOM,
          height: $TEXTCONFIG_HEIGHT,
          width: $TEXTCONFIG_WIDTH,
          color: '#ffffff',
          fontSize: 13,
        },
      },
      barConfig: {
        style: {
          width: $BARCONFIG_WIDTH,
          height: $BARCONFIG_HEIGHT,
          padding: 2,
          bottom: $BARCONFIG_BOTTOM,
          backgroundColor: '#ffffff',
        },
      },
      iconConfig: {
        visible: true,
        style: {
          width: $ICONCONFIG_WIDTH,
          height: $ICONCONFIG_HEIGHT,
          bottom: $ICONCONFIG_BOTTOM,
        },
      },
      materialConfig: {
        backgroundImage: 'images/background.png',
        iconImage: 'images/unity_logo.png',
      },
    },
  });

  // 启动自定义加载 UI 的渲染循环
  renderLoop(ctx, cw, ch);

  const gameManager = new UnityManager(canvas, managerConfig, unityNamespace);

  // 监听加载进度
  gameManager.onLaunchProgress((e) => {
    for (var i = 0; i < loadStages.length; i++) {
      if (e.type === loadStages[i].type) {
        currentProgress = loadStages[i].progress;
        currentText = loadStages[i].text;
        console.log('[Loading] ' + currentText + ' (' + Math.floor(currentProgress * 100) + '%)');
        break;
      }
    }
    if (e.type === launchEventType.prepareGame) {
      // 给 0.5 秒时间显示 "准备就绪"，然后标记完成
      setTimeout(function() { isComplete = true; }, 500);
    }
  });

  gameManager.onModulePrepared(() => {
    // unityModule has been called
  });

  gameManager.onLogError = function (err) {
    console.error(err);
  };

  globalThis.gameManager = gameManager;
  gameManager.startGame();
}

main();
