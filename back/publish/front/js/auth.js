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
        
        let emailHtml = email ? `<p>📧 ${email}</p>` : '';
        let phoneHtml = phone ? `<p>📞 ${phone}</p>` : '';
        
        authHtml = `
            <div class="notification-wrapper" style="margin-left: 15px; display: flex; align-items: center;">
                <button onclick="toggleNotificationDropdown()" style="background:none; border:none; color: var(--primary-gold); cursor:pointer; display: flex; align-items: center; padding: 4px; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.1)'" onmouseout="this.style.transform='scale(1)'">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M13.73 21a2 2 0 0 1-3.46 0"></path></svg>
                    <span id="notifBadge" class="notification-badge"></span>
                </button>
                <div id="notifDropdown" class="notification-dropdown">
                    <div class="notification-header">Notifications <button onclick="markAllRead()" style="background:none;border:none;color:var(--primary-gold);cursor:pointer;font-size:0.8rem;">Mark all as read</button></div>
                    <div id="notifItems">
                        <div style="padding:1rem;text-align:center;color:#777;">Loading...</div>
                    </div>
                    <div class="notification-footer"><a href="notifications.html?v=5">View All Notifications</a></div>
                </div>
            </div>
            <div class="profile-dropdown" style="display: flex; align-items: center;">
                <button onclick="toggleProfileCard()" style="background:none; border:none; color: var(--primary-gold); cursor:pointer; margin-left: 10px; display: flex; align-items: center; padding: 4px; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.1)'" onmouseout="this.style.transform='scale(1)'">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                </button>
                <div id="profileCard" class="profile-card">
                    <h4>${fname} ${lname}</h4>
                    ${emailHtml}
                    ${emailHtml || phoneHtml ? '' : '<p style="font-size:0.8rem; color:#888;">Welcome back!</p>'}
                    ${phoneHtml}
                    <a href="transactions.html" style="display:block; margin-top:10px; color:var(--primary-gold); font-size:0.9rem; text-decoration:none; font-weight:bold;">View Orders</a>
                    <button class="btn-logout" onclick="logout()">Logout</button>
                </div>
            </div>
        `;
    } else {
        // Logged out
        authHtml = `
            <a href="login.html" style="color: #ddd; text-decoration: none;">Login</a>
            <a href="signup.html" class="btn-register" style="background-color: var(--primary-gold); color: #111; padding: 6px 16px; border-radius: 4px; font-weight: 600; text-decoration: none;">Register</a>
        `;
    }

    // Standard links for all pages
    const standardLinks = `
        <a href="home.html" style="color: #ddd; text-decoration: none;">Home</a>
        <a href="products.html" style="color: #ddd; text-decoration: none;">Shop</a>
        <a href="about.html" style="color: #ddd; text-decoration: none;">About</a>
        <a href="contact.html" style="color: #ddd; text-decoration: none;">Contact</a>
        <a href="transactions.html" style="color: #ddd; text-decoration: none;">Orders</a>
        <a href="cart.html" style="color: var(--primary-gold); text-decoration: none; display: flex; align-items: center; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.1)'" onmouseout="this.style.transform='scale(1)'">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="21" r="1"></circle><circle cx="20" cy="21" r="1"></circle><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path></svg> 
            <span id="cartBadge" class="cart-badge" style="background: var(--primary-gold); color: #111; padding: 2px 6px; border-radius: 50%; font-size: 0.7rem; font-weight: bold; margin-left: 4px; display: none;"></span>
        </a>
    `;

    navLinks.innerHTML = standardLinks + authHtml;

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
                html += `<li><a href="products.html?category=${encodeURIComponent(c.name)}">${c.name}</a></li>`;
            });
            footerList.innerHTML = html;
        })
        .catch(err => {
            console.error("Dynamic categories fetch failed. Using fallback.", err);
            footerList.innerHTML = `
                <li><a href="products.html?category=Chappal">Chappal</a></li>
                <li><a href="products.html?category=Peshawari Chappal">Peshawari Chappal</a></li>
                <li><a href="products.html?category=Shoes">Shoes</a></li>
                <li><a href="products.html?category=Sandals">Sandals</a></li>
            `;
        });
    }

    // Inject CSS to ensure it always applies
    const style = document.createElement('style');
    style.textContent = `
        .profile-dropdown { position: relative; display: inline-block; }
        .profile-card { display: none; position: absolute; right: 0; top: 40px; background-color: #fff; min-width: 260px; max-height: 350px; overflow-y: auto; box-shadow: 0px 8px 24px rgba(0,0,0,0.15); z-index: 1000; border-radius: 8px; padding: 1.5rem; color: #333; text-align: left; border: 1px solid #eaeaea; }
        .profile-card.show { display: block; animation: slideDown 0.2s ease-out; }
        .profile-card h4 { margin: 0 0 10px 0; color: #2B1D17; font-family: 'Cinzel', serif; font-size: 1.2rem; }
        .profile-card p { margin: 8px 0; font-size: 0.9rem; color: #555; border-bottom: 1px solid #f0f0f0; padding-bottom: 8px; }
        .btn-logout { background-color: #8B2F2F; color: white; border: none; padding: 10px; width: 100%; border-radius: 6px; margin-top: 15px; cursor: pointer; font-weight: bold; font-family: 'Poppins', sans-serif; transition: background 0.2s; }
        .btn-logout:hover { background-color: #6a1a1a; }
        .btn-back { background: none; border: none; color: #C79A52; font-size: 1.1rem; cursor: pointer; padding: 10px 0; margin-bottom: 20px; font-weight: 600; font-family: 'Poppins', sans-serif; display: inline-flex; align-items: center; transition: transform 0.2s; }
        .btn-back:hover { transform: translateX(-5px); }
        @keyframes slideDown { from { opacity: 0; transform: translateY(-10px); } to { opacity: 1; transform: translateY(0); } }
    `;
    document.head.appendChild(style);

    // Insert back button globally except on home, login, signup, splash
    const path = window.location.pathname.toLowerCase();
    if (!path.includes('home.html') && !path.includes('login.html') && !path.includes('signup.html') && !path.includes('index.html')) {
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
            wrapper.style.padding = '10px 2rem 0';
            wrapper.style.boxSizing = 'border-box';
            wrapper.style.textAlign = 'left';
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
    window.location.href = 'login.html';
}

// Prevent Desktop Chrome Zoom (Ctrl + / Ctrl - / Ctrl + Wheel)
document.addEventListener('keydown', function(event) {
    if (event.ctrlKey && (event.key === '+' || event.key === '-' || event.key === '=')) {
        event.preventDefault();
    }
});
document.addEventListener('wheel', function(event) {
    if (event.ctrlKey) {
        event.preventDefault();
    }
}, { passive: false });
