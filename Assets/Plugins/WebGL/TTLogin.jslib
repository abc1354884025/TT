// TTLogin.jslib — 抖音小游戏 tt.login 桥接
var TTDebugLog = console.log;

mergeInto(LibraryManager.library, {
    // 调用 tt.login，结果通过 SendMessage 回调
    TTLogin: function(gameObjectName, callbackMethod) {
        var goName = UTF8ToString(gameObjectName);
        var method = UTF8ToString(callbackMethod);
        TTDebugLog('[TTLogin] 开始调用 tt.login');

        tt.login({
            success: function(res) {
                TTDebugLog('[TTLogin] 登录成功 code=' + res.code);
                // 构造 JSON 结果
                var result = JSON.stringify({ success: true, code: res.code || '', anonymousCode: res.anonymousCode || '' });
                // 回调到 Unity
                SendMessage(goName, method, result);
            },
            fail: function(res) {
                TTDebugLog('[TTLogin] 登录失败 err=' + JSON.stringify(res));
                var result = JSON.stringify({ success: false, errMsg: res.errMsg || 'login failed' });
                SendMessage(goName, method, result);
            }
        });
    }
});
