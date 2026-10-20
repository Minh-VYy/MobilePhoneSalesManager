(() => {
    const root = document.querySelector('[data-chat-root]');
    if (!root) return;
    const find = name => root.querySelector(`[data-chat-${name}]`);
    const admin = root.dataset.chatAdmin === 'true';
    const widget = root.dataset.chatWidget === 'true';
    const base = root.dataset.chatBase;
    const panel = root.querySelector('.chat-shell');
    const form = find('form'), input = find('text'), messages = find('messages'), scroll = find('scroll');
    const token = form.querySelector('[name="__RequestVerificationToken"]').value;
    let open = !widget, conversation = null, generation = 0, lastId = 0, firstId = 0, busy = false, loading = false, refreshPending = false;
    const rendered = new Set();
    let retry = null;
    let initialConversation = admin ? root.dataset.chatConversation : null;

    async function request(action, data) {
        const response = await fetch(base + action, data ? {
            method: 'POST', headers: { Accept: 'application/json' },
            body: new URLSearchParams({ __RequestVerificationToken: token, ...data })
        } : { headers: { Accept: 'application/json' }, cache: 'no-store' });
        if (response.status === 401) throw new Error('Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại để tiếp tục.');
        if (response.status === 429) throw new Error('Bạn thao tác hơi nhanh. Vui lòng đợi một chút rồi thử lại.');
        if (!response.ok) {
            const body = await response.json().catch(() => ({}));
            throw new Error(body.error || 'Chưa thể kết nối. Nội dung bạn nhập vẫn được giữ lại.');
        }
        return response.status === 204 ? null : response.json();
    }
    function error(message = '') { find('error').textContent = message; find('error').hidden = !message; }
    function messageNode(message) {
        const item = document.createElement('article');
        item.className = 'chat-message' + ((message.senderRole === 'Admin') === admin ? ' chat-message--own' : '');
        const name = document.createElement('b'); name.textContent = message.senderRole === 'Admin' ? `${message.senderName} · Nhân viên` : message.senderName;
        const text = document.createElement('p'); text.textContent = message.text;
        const time = document.createElement('time'); time.dateTime = message.sentAt; time.textContent = new Date(message.sentAt).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
        item.append(name, text, time);
        return item;
    }
    function append(items, earlier = false) {
        const atBottom = scroll.scrollHeight - scroll.scrollTop - scroll.clientHeight < 90;
        const height = scroll.scrollHeight;
        const fragment = document.createDocumentFragment();
        items.forEach(message => {
            if (rendered.has(message.id)) return;
            rendered.add(message.id); lastId = Math.max(lastId, message.id); firstId = firstId ? Math.min(firstId, message.id) : message.id;
            fragment.append(messageNode(message));
        });
        if (earlier) messages.prepend(fragment); else messages.append(fragment);
        find('empty').hidden = rendered.size !== 0;
        if (earlier) scroll.scrollTop += scroll.scrollHeight - height;
        else if (atBottom) scroll.scrollTop = scroll.scrollHeight;
    }
    async function markRead() {
        if (!open || !conversation || document.hidden || !lastId) return;
        await request('Read', { conversationId: conversation, through: String(lastId) });
    }
    async function loadMessages(earlier = false) {
        if (!open || (admin && !conversation)) return;
        if (loading) { if (!earlier) refreshPending = true; return; }
        loading = true;
        const id = conversation, version = generation;
        try {
            const params = new URLSearchParams({ conversationId: id || '', after: earlier ? '0' : String(lastId) });
            if (earlier && firstId) params.set('before', firstId);
            const data = await request('Messages?' + params);
            if (version !== generation) return;
            conversation = data.conversationId;
            append(data.messages, earlier);
            if (earlier || !firstId || firstId === data.messages[0]?.id) find('history').hidden = firstId <= 1 || data.messages.length < 50;
            find('send').disabled = false;
            await markRead();
            await loadInbox();
        } catch (err) { error(err.message); }
        finally { loading = false; if (refreshPending) { refreshPending = false; loadMessages(); } }
    }
    async function select(id, name) {
        generation++; conversation = id; lastId = firstId = 0; rendered.clear(); messages.replaceChildren();
        find('title').textContent = name; find('send').disabled = true; find('empty').hidden = false; find('history').hidden = true; error();
        loading = false;
        await loadMessages();
    }
    async function loadInbox() {
        const data = await request('Inbox');
        const badge = find('unread');
        if (badge) { badge.textContent = data.unread > 99 ? '99+' : String(data.unread); badge.hidden = !data.unread; }
        if (!admin) return;
        if (initialConversation) {
            const selected = data.conversations.find(item => item.id === initialConversation);
            initialConversation = null;
            if (selected) await select(selected.id, selected.customerName);
        }
        const inbox = find('inbox');
        const fragment = document.createDocumentFragment();
        data.conversations.forEach(item => {
            const button = document.createElement('button'); button.type = 'button'; button.className = 'chat-conversation' + (item.id === conversation ? ' is-active' : '');
            button.setAttribute('aria-pressed', String(item.id === conversation));
            const name = document.createElement('b'); name.textContent = item.customerName;
            const preview = document.createElement('span'); preview.textContent = item.preview;
            button.append(name, preview);
            if (item.unread) { const unread = document.createElement('small'); unread.textContent = `${item.unread} chưa đọc`; button.append(unread); }
            button.addEventListener('click', () => { input.value = ''; retry = null; select(item.id, item.customerName); });
            fragment.append(button);
        });
        if (!data.conversations.length) { const empty = document.createElement('p'); empty.className = 'chat-empty'; empty.textContent = 'Chưa có tin nhắn từ khách hàng.'; fragment.append(empty); }
        inbox.replaceChildren(fragment);
    }
    function toggle(show) {
        open = show; panel.hidden = !show;
        find('launch')?.setAttribute('aria-expanded', String(show));
        if (show) { error(); loadMessages(); input.focus(); }
        else find('launch')?.focus();
    }
    find('launch')?.addEventListener('click', () => toggle(!open));
    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-support-open]');
        if (!trigger || !widget) return;
        event.preventDefault(); toggle(true); input.value = trigger.dataset.supportMessage || ''; input.focus();
    });
    find('close')?.addEventListener('click', () => toggle(false));
    root.addEventListener('keydown', event => { if (widget && event.key === 'Escape') toggle(false); });
    find('history').addEventListener('click', () => loadMessages(true));
    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (busy || !input.value.trim() || (admin && !conversation)) return;
        const text = input.value.trim();
        if (!retry || retry.text !== text || retry.conversation !== conversation) retry = { text, conversation,
            clientId: crypto.randomUUID ? crypto.randomUUID() : Array.from(crypto.getRandomValues(new Uint8Array(16)), byte => byte.toString(16).padStart(2, '0')).join('') };
        busy = true; find('send').disabled = true; error();
        const version = generation;
        try {
            const data = await request('Send', { conversationId: conversation || '', text, clientId: retry.clientId });
            if (version !== generation) return;
            conversation = data.conversationId; append([data.message]); input.value = ''; retry = null;
            scroll.scrollTop = scroll.scrollHeight; await markRead(); await loadInbox();
        } catch (err) { error(err.message); }
        finally { busy = false; find('send').disabled = admin && !conversation; }
    });
    input.addEventListener('keydown', event => { if (event.key === 'Enter' && event.ctrlKey) { event.preventDefault(); form.requestSubmit(); } });
    let source;
    function connect() {
    source = new EventSource(base + 'Events');
    source.addEventListener('connected', () => { find('connection').textContent = 'Tin nhắn được lưu và cập nhật trực tiếp.'; loadInbox().catch(err => error(err.message)); if (open) loadMessages(); });
    source.addEventListener('change', event => {
        const data = JSON.parse(event.data);
        loadInbox().catch(err => error(err.message));
        if (open && (!admin || data.conversationId === conversation)) loadMessages();
    });
    source.onerror = () => { find('connection').textContent = 'Đang kết nối lại… Lịch sử tin nhắn đã được lưu.'; };
    }
    connect();
    document.addEventListener('visibilitychange', () => { if (!document.hidden) { loadInbox().catch(() => {}); if (open) loadMessages(); } });
    window.addEventListener('pagehide', () => source.close());
    window.addEventListener('pageshow', event => { if (event.persisted) connect(); });
    if (root.dataset.chatProduct) input.value = `Tôi muốn được tư vấn về sản phẩm ${root.dataset.chatProduct}.`;
})();
