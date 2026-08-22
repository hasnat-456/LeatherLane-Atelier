document.addEventListener('DOMContentLoaded', () => {
    const navLinks = document.querySelector('.nav-links');
    if (!navLinks) return;

    const token = localStorage.getItem('token');
    let authHtml = '';

    if (token) {
        // Logged in
        const fname = localStorage.getItem('profile_fname') || 'My Account';
        const lname = localStorage.getItem('profile_lname') || '';
        const email = localStorage.getItem('profile_email') || '';
        const phone = localStorage.getItem('profile_phone') || '';
        
        let emailHtml = email ? `<p><svg viewBox="0 0 24 24" width="1.2em" height="1.2em" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round" style="vertical-align: text-bottom; margin-right: 6px;"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path><polyline points="22,6 12,13 2,6"></polyline></svg> ${email}</p>` : '';
        let phoneHtml = phone ? `<p><svg viewBox="0 0 24 24" width="1.2em" height="1.2em" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round" style="vertical-align: text-bottom; margin-right: 6px;"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path></svg> ${phone}</p>` : '';
        
        authHtml = `
            <div class="notification-wrapper" style="display: flex; align-items: center;">
                <button onclick="toggleNotificationDropdown()" style="background:none; border:none; color: var(--primary-gold); cursor:pointer; display: flex; align-items: center; padding: 4px; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.1)'" onmouseout="this.style.transform='scale(1)'">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M13.73 21a2 2 0 0 1-3.46 0"></path></svg>
                    <span id="notifBadge" class="notification-badge"></span>
                </button>
                <div id="notifDropdown" class="notification-dropdown">
                    <div class="notification-header">Notifications <button onclick="markAllRead()" style="background:none;border:none;color:var(--primary-gold);cursor:pointer;font-size:0.8rem;">Mark all as read</button></div>
                    <div id="notifItems">
                        <div style="padding:1rem;text-align:center;color:#777;">Loading...</div>
                    </div>
                    <div class="notification-footer"><a href="/notifications?v=5">View All Notifications</a></div>
                </div>
            </div>
            <div class="profile-dropdown" style="display: flex; align-items: center;">
                <button onclick="toggleProfileCard()" style="background:none; border:none; color: var(--primary-gold); cursor:pointer; display: flex; align-items: center; padding: 4px; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.1)'" onmouseout="this.style.transform='scale(1)'">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                </button>
                <div id="profileCard" class="profile-card">
                    <h4>${fname} ${lname}</h4>
                    ${emailHtml}
                    ${emailHtml || phoneHtml ? '' : '<p style="font-size:0.8rem; color:#888;">Welcome back!</p>'}
                    ${phoneHtml}
                    <a href="/profile" style="display:block; margin-top:10px; color:var(--primary-gold); font-size:0.9rem; text-decoration:none; font-weight:bold;">My Profile</a>
                    <a href="/transactions" style="display:block; margin-top:10px; color:var(--primary-gold); font-size:0.9rem; text-decoration:none; font-weight:bold;">View Orders</a>
                    <button class="btn-logout" onclick="logout()">Logout</button>
                </div>
            </div>
        `;
    } else {
        // Logged out
        authHtml = `
            <a href="/login" style="color: #ddd; text-decoration: none;">Login</a>
            <a href="/signup" class="btn-register" style="background-color: var(--primary-gold); color: #111; padding: 6px 16px; border-radius: 4px; font-weight: 600; text-decoration: none;">Register</a>
        `;
    }

    // Standard links for all pages (text links only, icons go in their own row)
    const standardLinks = `
        <a href="/home" style="color: #ddd; text-decoration: none;">Home</a>
        <a href="/products" style="color: #ddd; text-decoration: none;">Shop</a>
        <a href="/about" style="color: #ddd; text-decoration: none;">About</a>
        <a href="/deals" style="color: #ddd; text-decoration: none;">Deal of the Day</a>
        <a href="/size-guide" style="color: #ddd; text-decoration: none;">Size Guide</a>
        <a href="/contact" style="color: #ddd; text-decoration: none;">Contact</a>
        <a href="/transactions" style="color: #ddd; text-decoration: none;">Orders</a>
        <a href="/favorites" style="color: #ddd; text-decoration: none;">Favorites</a>
    `;

    // Cart icon + auth icons wrapped in a single flex row so they're always horizontal
    const cartIconHtml = `
        <a href="/cart" style="color: var(--primary-gold); text-decoration: none; display: flex; align-items: center; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.1)'" onmouseout="this.style.transform='scale(1)'">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="21" r="1"></circle><circle cx="20" cy="21" r="1"></circle><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path></svg>
            <span id="cartBadge" class="cart-badge" style="background: var(--primary-gold); color: #111; padding: 2px 6px; border-radius: 50%; font-size: 0.7rem; font-weight: bold; margin-left: 4px; display: none;"></span>
        </a>
    `;

    const iconsRowHtml = `<div class="nav-icons-row" style="display:flex; flex-direction:row; align-items:center; gap:20px; flex-shrink:0;">${cartIconHtml}${authHtml}</div>`;

    navLinks.innerHTML = standardLinks + iconsRowHtml;

    // Update cart badge logic if not overridden by page
    const badge = document.getElementById('cartBadge');
    if (badge) {
        if (token) {
            fetch('/api/cart', {
                headers: { 'Authorization': `Bearer ${token}` }
            })
            .then(res => res.json())
            .then(cart => {
                if (cart && cart.length > 0) {
                    badge.style.display = 'inline-block';
                    let totalItems = 0;
                    cart.forEach(item => totalItems += (item.quantity || 1));
                    badge.innerText = totalItems;
                } else {
                    badge.style.display = 'none';
                }
            })
            .catch(err => {
                badge.style.display = 'none';
            });
        } else {
            badge.style.display = 'none';
        }
    }

    // Dynamic Footer Categories Loading
    const footerList = document.getElementById('footerCategoriesList');
    if (footerList) {
        fetch('/api/products/categories')
        .then(res => res.json())
        .then(cats => {
            let html = '';
            cats.forEach(c => {
                html += `<li><a href="/products?category=${encodeURIComponent(c.name)}">${c.name}</a></li>`;
            });
            footerList.innerHTML = html;
        })
        .catch(err => {
            console.error("Dynamic categories fetch failed. Using fallback.", err);
            footerList.innerHTML = `
                <li><a href="/products?category=Chappal">Chappal</a></li>
                <li><a href="/products?category=Peshawari Chappal">Peshawari Chappal</a></li>
                <li><a href="/products?category=Shoes">Shoes</a></li>
                <li><a href="/products?category=Sandals">Sandals</a></li>
            `;
        });
    }

    // --- Dynamic Settings Injection ---
    fetch('/api/settings')
        .then(res => res.json())
        .then(settings => {
            if (!settings) return;
            
            document.querySelectorAll('.dyn-address').forEach(el => el.textContent = settings.address || '');
            document.querySelectorAll('.dyn-email').forEach(el => el.textContent = settings.email || '');
            document.querySelectorAll('.dyn-phone').forEach(el => el.textContent = settings.phone || '');
            document.querySelectorAll('.dyn-hours').forEach(el => el.textContent = settings.businessHours || '');
            
            document.querySelectorAll('.dyn-social-fb').forEach(el => el.href = settings.facebookUrl || '#');
            document.querySelectorAll('.dyn-social-ig').forEach(el => el.href = settings.instagramUrl || '#');
            document.querySelectorAll('.dyn-social-wa').forEach(el => el.href = settings.whatsAppUrl || '#');
            document.querySelectorAll('.dyn-social-tk').forEach(el => el.href = settings.tikTokUrl || '#');
        })
        .catch(e => console.error("Could not load dynamic settings", e));

    
    // Inject CSS to ensure it always applies
    const style = document.createElement('style');
    style.textContent = `
        .profile-dropdown { position: relative; display: inline-block; }
        .profile-card { display: none; position: absolute; right: 0; top: 40px; background-color: #fff; min-width: 260px; max-height: 350px; overflow-y: auto; box-shadow: 0px 8px 24px rgba(0,0,0,0.15); z-index: 1000; border-radius: 8px; padding: 1.5rem; color: #333; text-align: left; border: 1px solid #eaeaea; }
        .profile-card.show { display: block; animation: slideDown 0.2s ease-out; }
        .profile-card h4 { margin: 0 0 10px 0; color: #470C0E; font-family: 'Cinzel', serif; font-size: 1.2rem; }
        .profile-card p { margin: 8px 0; font-size: 0.9rem; color: #555; border-bottom: 1px solid #f0f0f0; padding-bottom: 8px; }
        .btn-logout { background-color: #8B2F2F; color: white; border: none; padding: 10px; width: 100%; border-radius: 6px; margin-top: 15px; cursor: pointer; font-weight: bold; font-family: 'Poppins', sans-serif; transition: background 0.2s; }
        .btn-logout:hover { background-color: #6a1a1a; }
        .btn-back { background: none; border: none; color: #C79A52; font-size: 1.25rem; cursor: pointer; padding: 10px 0; margin-bottom: 20px; font-weight: 600; font-family: 'Poppins', sans-serif; display: inline-flex; align-items: center; transition: transform 0.2s; }
        .btn-back:hover { transform: translateX(-5px); }
        @keyframes slideDown { from { opacity: 0; transform: translateY(-10px); } to { opacity: 1; transform: translateY(0); } }
    `;
    document.head.appendChild(style);

    // Insert back button globally except on home, login, signup, splash
    const path = window.location.pathname.toLowerCase();
    if (!path.includes('/home') && !path.includes('/login') && !path.includes('/signup') && !path.includes('/index')) {
        // Prevent duplicate back buttons if one already exists
        if (document.querySelector('.btn-back')) {
            return;
        }
        const backBtn = document.createElement('button');
        backBtn.className = 'btn-back';
        backBtn.innerHTML = '&larr; Back';
        backBtn.onclick = () => window.history.back();
        
        // Place it directly under the navbar at the top-left
        const nav = document.querySelector('.navbar');
        if (nav && nav.nextElementSibling) {
            const wrapper = document.createElement('div');
            wrapper.style.width = '100%';
            wrapper.style.padding = '0.6rem 2.5rem 0.8rem 2.5rem';
            wrapper.style.boxSizing = 'border-box';
            wrapper.style.textAlign = 'left';
            wrapper.style.backgroundColor = 'var(--primary-bg)';
            wrapper.appendChild(backBtn);
            nav.parentNode.insertBefore(wrapper, nav.nextSibling);
        }
    }
});

function toggleProfileCard() {
    const card = document.getElementById('profileCard');
    if (card) {
        card.classList.toggle('show');
    }
}

function toggleNotificationDropdown() {
    const drop = document.getElementById('notifDropdown');
    if (drop) {
        drop.classList.toggle('show');
        if (drop.classList.contains('show')) {
            loadNotifications();
        }
    }
}

async function loadNotifications() {
    const token = localStorage.getItem('token');
    if (!token) return;

    try {
        const res = await fetch('/api/notifications', { headers: { 'Authorization': `Bearer ${token}` } });
        if (res.ok) {
            const notifs = await res.json();
            
            // Update badge
            const unreadCount = notifs.filter(n => !n.isRead).length;
            const badge = document.getElementById('notifBadge');
            if (badge) {
                if (unreadCount > 0) {
                    badge.innerText = unreadCount > 99 ? '99+' : unreadCount;
                    badge.style.display = 'inline-block';
                } else {
                    badge.style.display = 'none';
                }
            }

            // Render top 5
            const container = document.getElementById('notifItems');
            if (!container) return;

            if (notifs.length === 0) {
                container.innerHTML = '<div style="padding:1rem;text-align:center;color:#777;">No notifications.</div>';
                return;
            }

            let html = '';
            const top5 = notifs.slice(0, 5);
            top5.forEach(n => {
                const date = new Date(n.createdAt).toLocaleString();
                html += `
                    <a href="${n.actionUrl || '#'}" class="notification-item ${n.isRead ? '' : 'unread'}" onclick="markRead(${n.id})">
                        <div class="notification-item-title">${n.title}</div>
                        <div class="notification-item-msg">${n.message}</div>
                        <div class="notification-item-time">${date}</div>
                    </a>
                `;
            });
            container.innerHTML = html;
        }
    } catch (err) {
        console.error(err);
    }
}

async function markRead(id) {
    const token = localStorage.getItem('token');
    if (!token) return;
    await fetch(`/api/notifications/${id}/read`, { method: 'PUT', headers: { 'Authorization': `Bearer ${token}` } });
    loadNotifications();
}

async function markAllRead() {
    const token = localStorage.getItem('token');
    if (!token) return;
    await fetch(`/api/notifications/read-all`, { method: 'PUT', headers: { 'Authorization': `Bearer ${token}` } });
    loadNotifications();
}

// Auto-load badge on load
document.addEventListener('DOMContentLoaded', loadNotifications);

function logout() {
    localStorage.removeItem('token');
    window.location.href = '/login';
}



// --- WhatsApp Floating Button ---
document.addEventListener('DOMContentLoaded', async function() {
    if (document.getElementById('wa-floating-btn') || window.location.href.includes('admin')) return;

    let waNumber = '923376306162';
    
    // Fetch Admin Settings dynamically
    try {
        const res = await fetch('/api/settings');
        if (res.ok) {
            const settings = await res.json();
            if (settings && settings.whatsAppUrl && settings.whatsAppUrl.trim() !== '') {
                waNumber = settings.whatsAppUrl.trim();
            }
        }
    } catch(e) {
        console.error("Failed to fetch WhatsApp admin settings", e);
    }

    let waMessage = '';
    if (window.location.href.includes('/product-detail')) {
        let currentUrl = window.location.href;
        waMessage = '?text=' + encodeURIComponent('Hi, I need help with this: ' + currentUrl);
    }
    
    let waUrl = waNumber;
    if (!waUrl.startsWith('http')) {
        waUrl = 'https://wa.me/' + waNumber.replace(/[^\d\+]/g, '') + waMessage;
    } else {
        if (!waUrl.includes('?')) {
            waUrl += waMessage;
        } else {
            waUrl += waMessage.replace('?', '&');
        }
    }

    // Set up the footer WhatsApp icons to use the exact same link
    document.querySelectorAll("a[title='WhatsApp']").forEach(el => {
        el.href = waUrl;
        el.target = '_blank';
    });

    // Inject CSS
    const style = document.createElement('style');
    style.innerHTML = `
        .wa-floating-container {
            position: fixed;
            bottom: 30px;
            right: 30px;
            display: flex;
            align-items: center;
            gap: 15px;
            z-index: 90;
        }
        
        .wa-tooltip {
            background-color: white;
            color: #333;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 0.9rem;
            font-weight: bold;
            box-shadow: 0 4px 10px rgba(0,0,0,0.15);
            opacity: 0;
            transform: translateX(10px);
            transition: all 0.3s ease;
            pointer-events: none;
            white-space: nowrap;
        }
        
        .wa-floating-container:hover .wa-tooltip {
            opacity: 1;
            transform: translateX(0);
        }
        
        .wa-btn {
            background-color: #25D366;
            color: white;
            width: 70px;
            height: 70px;
            border-radius: 50%;
            display: flex;
            justify-content: center;
            align-items: center;
            box-shadow: 0 4px 15px rgba(37, 211, 102, 0.4);
            text-decoration: none;
            transition: transform 0.3s ease, box-shadow 0.3s ease;
        }
        
        .wa-btn:hover {
            transform: scale(1.1);
            box-shadow: 0 6px 20px rgba(37, 211, 102, 0.6);
        }
        
        .wa-btn svg {
            width: 42px;
            height: 42px;
            fill: currentColor;
        }
        
        @media (max-width: 768px) {
            .wa-floating-container {
                bottom: 20px;
                right: 20px;
            }
            .wa-tooltip {
                display: none; /* Hide tooltip on mobile for better UX */
            }
            .wa-btn {
                width: 60px;
                height: 60px;
            }
            .wa-btn svg {
                width: 35px;
                height: 35px;
            }
        }
    `;
    document.head.appendChild(style);

    // Create Elements
    const container = document.createElement('div');
    container.className = 'wa-floating-container';
    
    const tooltip = document.createElement('div');
    tooltip.className = 'wa-tooltip';
    tooltip.innerText = 'CHAT WITH US';
    
    const link = document.createElement('a');
    link.id = 'wa-floating-btn';
    link.className = 'wa-btn';
    link.href = waUrl;
    link.target = '_blank';
    link.rel = 'noopener noreferrer';
    link.innerHTML = '<svg viewBox="0 0 24 24"><path d="M.057 24l1.687-6.163c-1.041-1.804-1.588-3.849-1.587-5.946.003-6.556 5.338-11.891 11.893-11.891 3.181.001 6.167 1.24 8.413 3.488 2.245 2.245 3.481 5.236 3.48 8.414-.003 6.557-5.338 11.892-11.893 11.892-1.99-.001-3.951-.5-5.688-1.448l-6.305 1.654zm6.597-3.807c1.676.995 3.276 1.591 5.392 1.592 5.448 0 9.886-4.434 9.889-9.885.002-5.462-4.415-9.89-9.881-9.892-5.452 0-9.887 4.434-9.889 9.884-.001 2.225.651 3.891 1.746 5.634l-.999 3.648 3.742-.981zm11.387-5.464c-.074-.124-.272-.198-.57-.347-.297-.149-1.758-.868-2.031-.967-.272-.099-.47-.149-.669.149-.198.297-.768.967-.941 1.165-.173.198-.347.223-.644.074-.297-.149-1.255-.462-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.297-.347.446-.521.151-.172.2-.296.3-.495.099-.198.05-.372-.025-.521-.075-.148-.669-1.611-.916-2.206-.242-.579-.487-.501-.669-.51l-.57-.01c-.198 0-.52.074-.792.347-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.876 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.263.489 1.694.626.712.226 1.36.194 1.872.118.571-.085 1.758-.719 2.006-1.413.248-.695.248-1.29.173-1.414z"/></svg>';

    container.appendChild(tooltip);
    container.appendChild(link);
    document.body.appendChild(container);
});


// --- First-Time Login Notification Prompt ---
document.addEventListener('DOMContentLoaded', function() {
    const token = localStorage.getItem('token');
    const hasSeenPrompt = localStorage.getItem('has_seen_notif_prompt');
    const isAdminPage = window.location.href.includes('admin');

    if (token && !hasSeenPrompt && !isAdminPage) {
        // Inject CSS for Popup
        const style = document.createElement('style');
        style.innerHTML = `
            .notif-popup-overlay {
                position: fixed;
                bottom: 30px;
                left: 30px;
                background-color: #fff;
                border-radius: 12px;
                box-shadow: 0 10px 30px rgba(0,0,0,0.15);
                width: 320px;
                padding: 24px;
                z-index: 10000;
                font-family: 'Poppins', sans-serif;
                animation: slideUp 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
                border: 1px solid #eaeaea;
            }
            
            @keyframes slideUp {
                from { transform: translateY(50px); opacity: 0; }
                to { transform: translateY(0); opacity: 1; }
            }
            
            .notif-popup-header {
                display: flex;
                align-items: center;
                gap: 12px;
                margin-bottom: 15px;
            }
            
            .notif-popup-header svg {
                width: 24px;
                height: 24px;
                color: var(--primary-bg);
            }
            
            .notif-popup-header h4 {
                margin: 0;
                font-size: 1.1rem;
                font-weight: 700;
                color: #111;
                font-family: var(--font-heading, 'Cinzel', serif);
            }
            
            .notif-popup-desc {
                font-size: 0.85rem;
                color: #555;
                line-height: 1.5;
                margin-bottom: 20px;
                font-weight: 500;
            }
            
            .notif-popup-actions {
                display: flex;
                gap: 10px;
                align-items: center;
            }
            
            .btn-enable-notif {
                background-color: var(--primary-bg);
                color: #fff;
                border: none;
                padding: 10px 16px;
                border-radius: 6px;
                font-weight: 600;
                font-size: 0.85rem;
                cursor: pointer;
                transition: background 0.3s, transform 0.2s;
                flex: 1;
                text-align: center;
            }
            
            .btn-enable-notif:hover {
                background-color: var(--primary-gold);
                color: #111;
                transform: scale(1.02);
            }
            
            .btn-not-now {
                background-color: transparent;
                color: #888;
                border: none;
                padding: 10px 12px;
                font-weight: 600;
                font-size: 0.85rem;
                cursor: pointer;
                transition: color 0.3s;
            }
            
            .btn-not-now:hover {
                color: #333;
            }
            
            @media (max-width: 480px) {
                .notif-popup-overlay {
                    bottom: 0;
                    left: 0;
                    width: 100%;
                    border-radius: 20px 20px 0 0;
                    padding: 20px;
                    box-sizing: border-box;
                }
            }
        `;
        document.head.appendChild(style);

        // Create Popup Element
        const popup = document.createElement('div');
        popup.className = 'notif-popup-overlay';
        popup.id = 'notifOptInPopup';
        
        popup.innerHTML = `
            <div class="notif-popup-header">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                    <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
                </svg>
                <h4>STAY IN THE LOOP</h4>
            </div>
            <div class="notif-popup-desc">
                GET NOTIFIED ABOUT NEW DROPS, OFFERS, AND ORDER UPDATES.
            </div>
            <div class="notif-popup-actions">
                <button class="btn-enable-notif" id="btnEnableNotif">ENABLE NOTIFICATIONS</button>
                <button class="btn-not-now" id="btnNotNowNotif">NOT NOW</button>
            </div>
        `;
        
        document.body.appendChild(popup);
        
        // Handle Interactions
        document.getElementById('btnEnableNotif').addEventListener('click', function() {
            localStorage.setItem('has_seen_notif_prompt', 'true');
            localStorage.setItem('notifications_enabled', 'true');
            
            // Request native browser permission
            if ('Notification' in window) {
                Notification.requestPermission().then(permission => {
                    console.log('Native Notification permission:', permission);
                });
            }
            
            popup.style.display = 'none';
        });
        
        document.getElementById('btnNotNowNotif').addEventListener('click', function() {
            localStorage.setItem('has_seen_notif_prompt', 'true');
            localStorage.setItem('notifications_enabled', 'false');
            popup.style.display = 'none';
        });
    }
});


// Newsletter Subscription Logic
document.addEventListener('DOMContentLoaded', () => {
    const newsletterForm = document.getElementById('newsletter-form');
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const emailInput = document.getElementById('newsletter-email');
            const msgDiv = document.getElementById('newsletter-msg');
            const email = emailInput.value.trim();
            
            if (!email) return;

            try {
                const res = await fetch('/api/newsletter/subscribe', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email })
                });

                const data = await res.json();
                
                msgDiv.style.display = 'block';
                if (res.ok) {
                    msgDiv.style.color = '#28a745';
                    msgDiv.textContent = 'Thank you for subscribing!';
                    emailInput.value = '';
                } else {
                    msgDiv.style.color = '#dc3545';
                    msgDiv.textContent = data.message || 'Subscription failed.';
                }
                
                setTimeout(() => { msgDiv.style.display = 'none'; }, 5000);
            } catch (err) {
                console.error(err);
                msgDiv.style.display = 'block';
                msgDiv.style.color = '#dc3545';
                msgDiv.textContent = 'Network error. Try again.';
            }
        });
    }
});
