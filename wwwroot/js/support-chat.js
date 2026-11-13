(() => {
    const root = document.querySelector('[data-chat-root]');
    if (!root) return;
    const find = name => root.querySelector('[data-chat-' + name + ']');
    const admin = root.dataset.chatAdmin === 'true', widget = root.dataset.chatWidget === 'true';
    const base = root.dataset.chatBase, panel = root.querySelector('.chat-shell');
    const form = find('form'), input = find('text'), messages = find('messages'), scroll = find('scroll');
    const token = form.querySelector('[name="__RequestVerificationToken"]').value;
    const picker = find('picker'), pickButtons = Array.from(root.querySelectorAll('[data-chat-pick]'));
    const money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 });
    let open = !widget, conversation = null, generation = 0, lastId = 0, firstId = 0;
    let busy = false, contextVersion = 0, contextReady = false, contextPending = 0, contextEpoch = 0;
    let loading = null, refreshPending = false, retry = null, selectionQueue = Promise.resolve();
    let pickerKind = 'Product', pickerSequence = 0, pickerTimer;
    let hasActiveOrders = false;
    let initialConversation = admin ? root.dataset.chatConversation : null;
    let initialReference = root.dataset.chatOrder ? { kind: 'Order', id: root.dataset.chatOrder }
        : root.dataset.chatProduct ? { kind: 'Product', id: root.dataset.chatProduct } : null;
    const rendered = new Set();
    let stickToBottom = true, scrollFrame = 0;
    function keepLatestVisible() {
        cancelAnimationFrame(scrollFrame);
        scrollFrame = requestAnimationFrame(() => { if (open && stickToBottom) scroll.scrollTop = scroll.scrollHeight; });
    }
    scroll.addEventListener('scroll', () => { stickToBottom = scroll.scrollHeight - scroll.scrollTop - scroll.clientHeight < 60; }, { passive: true });
    const resize = new ResizeObserver(keepLatestVisible);
    resize.observe(scroll); resize.observe(messages);
    const uuid = () => crypto.randomUUID ? crypto.randomUUID()
        : Array.from(crypto.getRandomValues(new Uint8Array(16)), byte => byte.toString(16).padStart(2, '0')).join('');

    async function request(action, data) {
        const response = await fetch(base + action, data ? {
            method: 'POST', headers: { Accept: 'application/json' },
            body: new URLSearchParams({ __RequestVerificationToken: token, ...data })
        } : { headers: { Accept: 'application/json' }, cache: 'no-store' });
        if (response.status === 401) throw new Error('Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại để tiếp tục.');
        if (response.status === 403) throw new Error('Bạn không có quyền thực hiện thao tác này.');
        if (response.status === 429) throw new Error('Bạn thao tác hơi nhanh. Vui lòng đợi một chút rồi thử lại.');
        if (!response.ok) {
            const body = await response.json().catch(() => ({}));
            throw new Error(body.error || 'Chưa thể kết nối. Nội dung bạn nhập vẫn được giữ lại.');
        }
        return response.status === 204 ? null : response.json();
    }
    function error(message = '') { find('error').textContent = message; find('error').hidden = !message; }
    function controls() {
        const disabled = busy || contextPending > 0 || !contextReady || (admin && !conversation);
        find('send').disabled = disabled;
        find('context-clear').disabled = disabled;
        pickButtons.forEach(button => {
            button.disabled = disabled;
            if (button.dataset.chatPick === 'Order') button.hidden = !hasActiveOrders;
        });
        if (!hasActiveOrders && picker.open && pickerKind === 'Order') {
            picker.close();
            find('picker-list').replaceChildren();
        }
        find('picker-list').querySelectorAll('button').forEach(button => { button.disabled = contextPending > 0; });
    }
    function safeUrl(value) {
        try { return new URL(value, location.href); } catch { return null; }
    }
    // Only same-project detail links are used; all display content is inserted as text.
    function contextCard(context, selectable = false, pinned = false) {
        const card = document.createElement(selectable ? 'button' : 'a');
        card.className = 'chat-reference-card';
        card.title = [context.title, context.referenceId, context.description, context.status].filter(Boolean).join(' · ');
        if (selectable) card.type = 'button';
        else if (context.url) {
            const url = safeUrl(context.url);
            if (url?.origin === location.origin) { card.href = url.href; card.target = '_blank'; card.rel = 'noopener'; }
        }
        if (context.image) {
            const url = safeUrl(context.image.replace(/^~\//, '/'));
            if (url && ['http:', 'https:'].includes(url.protocol)) {
                const img = document.createElement('img'); img.src = url.href; img.alt = ''; img.loading = 'lazy';
                img.addEventListener('error', () => { img.hidden = true; }, { once: true }); card.append(img);
            }
        }
        const info = document.createElement('span'); info.className = 'chat-reference-info';
        const kind = document.createElement('small'); kind.textContent = context.kind === 'Order' ? 'Đơn hàng · ' + context.status : context.referenceId + (pinned ? ' · ' + context.status : '');
        const title = document.createElement('strong'); title.textContent = context.title;
        const description = document.createElement('span'); description.className = 'chat-reference-description'; description.textContent = context.description;
        const value = document.createElement('b'); value.textContent = money.format(context.amount);
        const status = document.createElement('small'); status.textContent = context.kind === 'Product' ? context.status : 'Tiền sản phẩm';
        info.append(kind, title, description, value, status); card.append(info);
        if (selectable) {
            const pick = document.createElement('span'); pick.className = 'chat-reference-pick'; pick.textContent = 'Chọn'; card.append(pick);
            card.setAttribute('aria-label', 'Chọn ' + context.title + ', ' + context.status);
        } else if (context.url) card.setAttribute('aria-label', context.title + '. Mở chi tiết trong tab mới.');
        return card;
    }
    function applyState(data) {
        if (data.contextVersion < contextVersion) return;
        const signature = JSON.stringify(data.context);
        if (find('context').dataset.signature !== signature) {
            find('context').dataset.signature = signature;
            find('context-card').replaceChildren();
            if (data.context) find('context-card').append(contextCard(data.context, false, true));
        }
        find('context').hidden = !data.context;
        contextVersion = data.contextVersion;
        hasActiveOrders = data.hasActiveOrders === true;
        contextReady = true;
        controls();
        keepLatestVisible();
    }
    function messageNode(message) {
        const item = document.createElement('article');
        item.className = 'chat-message' + ((message.senderRole === 'Admin') === admin ? ' chat-message--own' : '');
        if (message.kind && message.kind !== 'Text') item.classList.add('chat-message--context');
        const name = document.createElement('b'); name.textContent = message.senderRole === 'Admin' ? message.senderName + ' · Nhân viên' : message.senderName;
        const text = document.createElement('p'); text.textContent = message.text;
        const time = document.createElement('time'); time.dateTime = message.sentAt;
        time.textContent = new Date(message.sentAt).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
        item.append(name, text);
        if (message.context) item.append(contextCard(message.context));
        item.append(time);
        return item;
    }
    function append(items, earlier = false) {
        const atBottom = stickToBottom;
        if (earlier) stickToBottom = false;
        const height = scroll.scrollHeight, fragment = document.createDocumentFragment();
        items.forEach(message => {
            if (rendered.has(message.id)) return;
            rendered.add(message.id); lastId = Math.max(lastId, message.id); firstId = firstId ? Math.min(firstId, message.id) : message.id;
            fragment.append(messageNode(message));
        });
        if (earlier) messages.prepend(fragment); else messages.append(fragment);
        find('empty').hidden = rendered.size !== 0;
        if (earlier) scroll.scrollTop += scroll.scrollHeight - height;
        else if (atBottom) keepLatestVisible();
    }
    async function markRead() {
        if (!open || !conversation || document.hidden || !lastId) return;
        await request('Read', { conversationId: conversation, through: String(lastId) });
    }
    async function loadMessages(earlier = false) {
        if (!open || (admin && !conversation)) return;
        if (loading?.generation === generation) { if (!earlier) refreshPending = true; return; }
        const task = { generation }, id = conversation, epoch = contextEpoch;
        loading = task;
        try {
            const params = new URLSearchParams({ conversationId: id || '', after: earlier ? '0' : String(lastId) });
            if (earlier && firstId) params.set('before', firstId);
            const data = await request('Messages?' + params);
            if (task.generation !== generation) return;
            conversation = data.conversationId;
            append(data.messages, earlier);
            if (epoch === contextEpoch) applyState(data);
            if (!earlier && picker.open && pickerKind === 'Order') loadChoices();
            if (earlier || !firstId || firstId === data.messages[0]?.id) find('history').hidden = firstId <= 1 || data.messages.length < 50;
            await markRead();
            await loadInbox();
        } catch (err) { if (task.generation === generation) error(err.message); }
        finally {
            if (loading === task) loading = null;
            if (task.generation === generation) {
                controls();
                if (initialReference && contextReady) {
                    const reference = initialReference; initialReference = null;
                    chooseReference(reference.kind, reference.id);
                } else if (refreshPending) { refreshPending = false; loadMessages(); }
            }
        }
    }
    async function select(id, name) {
        stickToBottom = true;
        generation++; conversation = id; lastId = firstId = 0; contextVersion = 0; contextReady = false; hasActiveOrders = false; retry = null; refreshPending = false;
        rendered.clear(); messages.replaceChildren(); find('context').hidden = true; delete find('context').dataset.signature;
        picker.close(); pickerSequence++;
        find('picker-list').replaceChildren();
        find('title').textContent = name; find('empty').hidden = false; find('history').hidden = true; error(); controls();
        await loadMessages();
    }
    async function loadInbox() {
        const data = await request('Inbox'), badge = find('unread');
        if (badge) { badge.textContent = data.unread > 99 ? '99+' : String(data.unread); badge.hidden = !data.unread; }
        if (!admin) return;
        if (initialConversation) {
            const selected = data.conversations.find(item => item.id === initialConversation);
            initialConversation = null;
            if (selected) await select(selected.id, selected.customerName);
        }
        const inbox = find('inbox'), fragment = document.createDocumentFragment();
        data.conversations.forEach(item => {
            const button = document.createElement('button'); button.type = 'button';
            button.className = 'chat-conversation' + (item.id === conversation ? ' is-active' : '');
            button.setAttribute('aria-pressed', String(item.id === conversation));
            const name = document.createElement('b'); name.textContent = item.customerName;
            const preview = document.createElement('span'); preview.textContent = item.preview; button.append(name, preview);
            if (item.unread) { const unread = document.createElement('small'); unread.textContent = item.unread + ' chưa đọc'; button.append(unread); }
            button.addEventListener('click', () => { if (item.id !== conversation) { input.value = ''; select(item.id, item.customerName); } });
            fragment.append(button);
        });
        if (!data.conversations.length) { const empty = document.createElement('p'); empty.className = 'chat-empty'; empty.textContent = 'Chưa có tin nhắn từ khách hàng.'; fragment.append(empty); }
        inbox.replaceChildren(fragment);
    }
    function chooseReference(kind, id) {
        if (admin && !conversation) return;
        const version = generation, target = conversation, epoch = ++contextEpoch, clientId = uuid();
        contextPending++; contextReady = false; controls(); error();
        selectionQueue = selectionQueue.then(async () => {
            try {
                if (version !== generation) return;
                const data = await request('SelectContext', { conversationId: target || '', kind, referenceId: id || '', clientId });
                if (version !== generation) return;
                conversation = data.conversationId;
                if (epoch === contextEpoch) applyState(data);
                picker.close();
            } catch (err) { if (version === generation) error(err.message); }
            finally {
                contextPending--; controls();
                if (version === generation) loadMessages();
            }
        });
        return selectionQueue;
    }
    async function loadChoices() {
        clearTimeout(pickerTimer);
        if (!picker.open) return;
        const sequence = ++pickerSequence, version = generation, target = conversation;
        find('picker-list').replaceChildren();
        find('picker-status').textContent = 'Đang lấy danh sách…';
        try {
            const params = new URLSearchParams({ conversationId: target || '', kind: pickerKind, keyword: find('picker-keyword').value.trim() });
            const data = await request('ContextOptions?' + params);
            if (version !== generation || sequence !== pickerSequence || !picker.open) return;
            const fragment = document.createDocumentFragment();
            data.items.forEach(item => {
                const card = contextCard(item, true);
                card.addEventListener('click', () => chooseReference(item.kind, item.referenceId));
                fragment.append(card);
            });
            find('picker-list').replaceChildren(fragment);
            controls();
            find('picker-status').textContent = data.items.length ? data.items.length + ' kết quả. Chọn để hiển thị cho cả hai người.' : pickerKind === 'Order'
                ? 'Không có đơn đang xử lý hoặc đang giao phù hợp của khách này.' : 'Không tìm thấy sản phẩm phù hợp.';
        } catch (err) { if (sequence === pickerSequence && version === generation) find('picker-status').textContent = err.message; }
    }
    pickButtons.forEach(button => button.addEventListener('click', () => {
        pickerKind = button.dataset.chatPick;
        find('picker-title').textContent = pickerKind === 'Order' ? 'Chọn đơn hàng cần hỗ trợ' : 'Chọn sản phẩm cần tư vấn';
        find('picker-note').textContent = pickerKind === 'Order' ? 'Chỉ hiện đơn của khách trong cuộc trò chuyện còn chờ xác nhận, đã xác nhận hoặc đang giao. Không hiện đơn đã giao hay đã hủy.' : 'Chọn đúng phiên bản, dung lượng và màu đang quan tâm.';
        find('picker-keyword').value = ''; picker.showModal(); loadChoices(); find('picker-keyword').focus();
    }));
    find('picker-close').addEventListener('click', () => picker.close());
    picker.addEventListener('close', () => { clearTimeout(pickerTimer); pickerSequence++; });
    find('picker-search').addEventListener('submit', event => { event.preventDefault(); loadChoices(); });
    find('picker-keyword').addEventListener('input', () => { clearTimeout(pickerTimer); pickerSequence++; pickerTimer = setTimeout(loadChoices, 250); });
    find('context-clear').addEventListener('click', () => chooseReference('Clear', ''));

    function toggle(show) {
        open = show; panel.hidden = !show;
        find('launch')?.setAttribute('aria-expanded', String(show));
        if (show) { error(); loadMessages(); input.focus(); }
        else { picker.close(); find('launch')?.focus(); }
    }
    find('launch')?.addEventListener('click', () => toggle(!open));
    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-support-open]');
        if (!trigger || !widget) return;
        event.preventDefault(); toggle(true);
        if (trigger.dataset.supportMessage && !input.value.trim()) input.value = trigger.dataset.supportMessage;
        const url = new URL(trigger.href, location.href);
        const orderId = trigger.dataset.supportOrder || url.searchParams.get('orderId');
        const productId = trigger.dataset.supportProduct || url.searchParams.get('productId');
        if (orderId || productId) chooseReference(orderId ? 'Order' : 'Product', orderId || productId);
        input.focus();
    });
    find('close')?.addEventListener('click', () => toggle(false));
    root.addEventListener('keydown', event => { if (widget && event.key === 'Escape' && !picker.open) toggle(false); });
    find('history').addEventListener('click', () => loadMessages(true));
    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (busy || contextPending || !contextReady || !input.value.trim() || (admin && !conversation)) return;
        const text = input.value.trim();
        if (!retry || retry.text !== text || retry.conversation !== conversation)
            retry = { text, conversation, contextVersion, clientId: uuid() };
        else retry.contextVersion = contextVersion;
        busy = true; controls(); error();
        const version = generation;
        try {
            const data = await request('Send', { conversationId: conversation || '', text, clientId: retry.clientId, contextVersion: String(retry.contextVersion) });
            if (version !== generation) return;
            conversation = data.conversationId;
            if (input.value.trim() === text) input.value = '';
            retry = null; stickToBottom = true; keepLatestVisible();
            await markRead(); await loadMessages();
        } catch (err) {
            if (version === generation) { error(err.message); loadMessages(); }
        } finally { busy = false; controls(); }
    });
    input.addEventListener('keydown', event => { if (event.key === 'Enter' && event.ctrlKey) { event.preventDefault(); form.requestSubmit(); } });

    let source;
    function connect() {
        source?.close();
        source = new EventSource(base + 'Events');
        source.addEventListener('connected', () => {
            find('connection').textContent = 'Tin nhắn và nội dung tư vấn được lưu, cập nhật cho cả hai người.';
            loadInbox().catch(err => error(err.message)); if (open) loadMessages();
        });
        source.addEventListener('change', event => {
            const data = JSON.parse(event.data);
            loadInbox().catch(err => error(err.message));
            if (open && (!admin || data.conversationId === conversation)) loadMessages();
        });
        source.onerror = () => { find('connection').textContent = 'Đang kết nối lại… Lịch sử tin nhắn đã được lưu.'; };
    }
    connect();
    document.addEventListener('visibilitychange', () => { if (!document.hidden) { loadInbox().catch(() => {}); if (open) loadMessages(); } });
    window.addEventListener('pagehide', () => { clearTimeout(pickerTimer); cancelAnimationFrame(scrollFrame); resize.disconnect(); source.close(); });
    window.addEventListener('pageshow', event => { if (event.persisted) { resize.observe(scroll); resize.observe(messages); connect(); } });
})();
