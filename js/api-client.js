/**
 * IdleQuest API helpers — matches IdleQuest.Backend routes.
 * Include after login flows; set idleQuestApi.API_BASE_URL if your API port differs.
 */
(function (global) {
    var API_BASE_URL = 'http://localhost:5098/api';

    async function requestWithToken(url, method, body) {
        method = method || 'GET';
        var token = localStorage.getItem('token');
        var headers = { 'Content-Type': 'application/json' };
        if (token) headers['Authorization'] = 'Bearer ' + token;
        var opts = { method: method, headers: headers };
        if (body != null && method !== 'GET') opts.body = JSON.stringify(body);
        var response = await fetch(url, opts);
        if (!response.ok) {
            var err = {};
            try { err = await response.json(); } catch (e) {}
            throw new Error(err.error || response.statusText);
        }
        var ct = response.headers.get('content-type');
        if (ct && ct.indexOf('application/json') !== -1) return response.json();
        return null;
    }

    global.idleQuestApi = {
        API_BASE_URL: API_BASE_URL,
        login: function (u, p) {
            return requestWithToken(API_BASE_URL + '/auth/login', 'POST', { username: u, password: p }).then(function (data) {
                if (data && data.token) localStorage.setItem('token', data.token);
                return data;
            });
        },
        register: function (username, password, heroName, characterClass) {
            return requestWithToken(API_BASE_URL + '/auth/register', 'POST', {
                username: username,
                password: password,
                heroName: heroName,
                class: characterClass
            });
        },
        logout: function () { localStorage.removeItem('token'); },
        getPlayerMe: function () { return requestWithToken(API_BASE_URL + '/player/me'); },
        renameHero: function (name) { return requestWithToken(API_BASE_URL + '/player/me/rename', 'PATCH', { name: name }); },
        startCombat: function (zoneId) { return requestWithToken(API_BASE_URL + '/combat/start/' + zoneId, 'POST'); },
        combatAttack: function (sessionId) { return requestWithToken(API_BASE_URL + '/combat/' + sessionId + '/attack', 'POST'); },
        combatFlee: function (sessionId) { return requestWithToken(API_BASE_URL + '/combat/' + sessionId + '/flee', 'POST'); },
        claimIdleRewards: function () { return requestWithToken(API_BASE_URL + '/combat/idle-rewards', 'POST'); },
        worldZones: function () { return requestWithToken(API_BASE_URL + '/world/zones'); },
        travel: function (zoneId) { return requestWithToken(API_BASE_URL + '/world/zones/' + zoneId + '/travel', 'POST'); },
        getInventory: function () { return requestWithToken(API_BASE_URL + '/inventory'); },
        equipItem: function (itemId) { return requestWithToken(API_BASE_URL + '/inventory/equip/' + itemId, 'POST'); },
        unequipItem: function (slot) { return requestWithToken(API_BASE_URL + '/inventory/equip/' + slot, 'DELETE'); },
        questsAvailable: function () { return requestWithToken(API_BASE_URL + '/quests/available'); }
    };
})(typeof window !== 'undefined' ? window : globalThis);
