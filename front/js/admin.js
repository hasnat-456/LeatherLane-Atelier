// Initialization
let editingProductId = null;

// Tab Switching
function switchTab(tabId) {
    // Update nav items
    document.querySelectorAll('.admin-nav .nav-item').forEach(item => {
        item.classList.remove('active');
        if (item.getAttribute('onclick') && item.getAttribute('onclick').includes(tabId)) {
            item.classList.add('active');
        }
    });
    
    // Update sections
    document.querySelectorAll('.section').forEach(sec => sec.classList.remove('active'));
    const sec = document.getElementById(tabId);
    if (sec) sec.classList.add('active');
    
    // Action-specific fetch calls
    if (tabId === 'payment-verification') {
        fetchPendingPayments();
    } else if (tabId === 'settings') {
        fetchManualPaymentSettings();
    } else if (tabId === 'categories') {
        fetchCategoriesList();
    } else if (tabId === 'dashboard') {
        fetchDashboardStats();
    } else if (tabId === 'orders') {
        fetchOrders();
    } else if (tabId === 'products') {
        fetchProducts();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    fetchDashboardStats();
    fetchOrders();
    fetchProducts();
    fetchPendingPayments();
    fetchManualPaymentSettings();
    fetchCategoriesList();

    // Check hash for direct navigation
    if (window.location.hash) {
        const tab = window.location.hash.substring(1);
        switchTab(tab);
    }
});

// Authentication
function adminLogout() {
    localStorage.removeItem('token');
    window.location.href = 'login.html';
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
                container.innerHTML = '<div style="padding:1rem;text-align:center;color:#777;">No alerts.</div>';
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

document.addEventListener('DOMContentLoaded', () => {
    loadNotifications();
});

async function changePassword() {
    const newEmail = document.getElementById('newEmail').value.trim();
    const newP = document.getElementById('newPwd').value;
    const msg = document.getElementById('pwdMsg');
    
    msg.style.display = 'block';
    
    if (newP && newP.length < 6) {
        msg.style.color = 'var(--error)';
        msg.innerText = 'New password must be at least 6 characters.';
        return;
    }

    if (!newEmail && !newP) {
        msg.style.color = 'var(--error)';
        msg.innerText = 'Please provide a new email or password to update.';
        return;
    }

    try {
        const res = await fetch('/api/adminapi/settings', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: newEmail, newPassword: newP })
        });

        if (res.ok) {
            msg.style.color = 'var(--success)';
            msg.innerText = 'Credentials updated successfully.';
            document.getElementById('newEmail').value = '';
            document.getElementById('newPwd').value = '';
        } else {
            msg.style.color = 'var(--error)';
            msg.innerText = 'Failed to update credentials.';
        }
    } catch (e) {
        msg.style.color = 'var(--error)';
        msg.innerText = 'Network error.';
    }
}

async function uploadAboutImage() {
    const fileInput = document.getElementById('aboutImageFile');
    const msg = document.getElementById('imgMsg');
    
    if (!fileInput.files || fileInput.files.length === 0) return;
    
    msg.style.display = 'block';
    msg.style.color = '#333';
    msg.innerText = 'Uploading...';
    
    const formData = new FormData();
    formData.append('imageFile', fileInput.files[0]);
    formData.append('target', 'about-image.jpg');
    
    try {
        const res = await fetch('/api/adminapi/site-image', {
            method: 'POST',
            body: formData
        });
        
        if (res.ok) {
            msg.style.color = 'var(--success)';
            msg.innerText = 'Image uploaded successfully!';
            document.getElementById('aboutImgForm').reset();
        } else {
            msg.style.color = 'var(--error)';
            msg.innerText = 'Upload failed.';
        }
    } catch (e) {
        msg.style.color = 'var(--error)';
        msg.innerText = 'Network error.';
    }
}

// Data Management
async function fetchDashboardStats() {
    try {
        const res = await fetch('/api/adminapi/stats');
        if (res.ok) {
            const data = await res.json();
            document.getElementById('statOrders').innerText = data.totalOrders;
            document.getElementById('statRevenue').innerText = '$' + data.totalRevenue.toFixed(2);
        }
    } catch (err) {
        console.error("Error fetching stats", err);
    }
}

// Orders UI
async function fetchOrders() {
    try {
        const res = await fetch('/api/adminapi/orders');
        if (res.ok) {
            const orders = await res.json();
            renderOrders(orders);
        }
    } catch (err) {
        console.error("Error fetching orders", err);
    }
}

function renderOrders(orders) {
    const tbody = document.getElementById('ordersTableBody');
    let html = '';
    
    orders.forEach(o => {
        const dateStr = new Date(o.date).toLocaleDateString();
        const statusClassMap = {
            'order placed': 'badge-processing',
            'order confirmed': 'badge-processing',
            'preparing order': 'badge-processing',
            'packed': 'badge-dispatched',
            'handed to courier': 'badge-dispatched',
            'in transit': 'badge-dispatched',
            'out for delivery': 'badge-dispatched',
            'delivered': 'badge-delivered'
        };
        const st = o.status.toLowerCase();
        const badgeClass = statusClassMap[st] || 'badge-processing';
        const displayStatus = o.status;
        
        html += `
            <tr>
                <td style="font-weight: bold;">${o.id}</td>
                <td>${dateStr}</td>
                <td>${o.customer}</td>
                <td>Rs. ${o.amount.toFixed(2)}</td>
                <td><span class="badge ${badgeClass}">${displayStatus}</span></td>
                <td>
                    <select class="status-select" onchange="updateOrderStatus('${o.id}', this.value)" ${o.status === 'Cancelled' ? 'disabled style="background-color: #eaeaea; cursor: not-allowed;"' : ''}>
                        <option value="Order Placed" ${o.status === 'Order Placed' ? 'selected' : ''}>Order Placed</option>
                        <option value="Order Confirmed" ${o.status === 'Order Confirmed' ? 'selected' : ''}>Order Confirmed</option>
                        <option value="Preparing Order" ${o.status === 'Preparing Order' ? 'selected' : ''}>Preparing Order</option>
                        <option value="Packed" ${o.status === 'Packed' ? 'selected' : ''}>Packed</option>
                        <option value="Handed to Courier" ${o.status === 'Handed to Courier' ? 'selected' : ''}>Handed to Courier</option>
                        <option value="In Transit" ${o.status === 'In Transit' ? 'selected' : ''}>In Transit</option>
                        <option value="Out for Delivery" ${o.status === 'Out for Delivery' ? 'selected' : ''}>Out for Delivery</option>
                        <option value="Delivered" ${o.status === 'Delivered' ? 'selected' : ''}>Delivered</option>
                        <option value="Cancelled" ${o.status === 'Cancelled' ? 'selected' : ''}>Cancelled</option>
                    </select>
                </td>
            </tr>
        `;
    });
    tbody.innerHTML = html;
}

async function updateOrderStatus(id, newStatus) {
    try {
        const res = await fetch(`/api/adminapi/orders/${id}/status`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ status: newStatus })
        });
        if (res.ok) {
            fetchOrders();
            fetchDashboardStats();
        } else {
            alert('Failed to update status');
        }
    } catch (err) {
        alert('Error updating status');
    }
}

// Products UI
async function fetchProducts() {
    try {
        const res = await fetch('/api/products');
        if (res.ok) {
            const products = await res.json();
            renderProducts(products);
        }
    } catch (err) {
        console.error("Error fetching products", err);
    }
}

function renderProducts(products) {
    const tbody = document.getElementById('productsTableBody');
    let html = '';
    
    products.forEach(p => {
        let badgeColor = '#2e6b4d';
        const status = p.availabilityStatus || 'Available';
        if (status === 'Temporarily Unavailable') badgeColor = '#e0a800';
        else if (status === 'Discontinued') badgeColor = '#dc3545';

        html += `
            <tr>
                <td><img src="${p.thumbnail || 'https://via.placeholder.com/50'}" alt="${p.name}" style="width: 50px; height: 50px; object-fit: cover; border-radius: 4px;"></td>
                <td style="font-weight: 500;">${p.name}</td>
                <td>${p.category}</td>
                <td>Rs. ${p.price.toFixed(2)}</td>
                <td><span style="background-color: ${badgeColor}; color: #fff; padding: 2px 8px; border-radius: 4px; font-size: 0.8rem; font-weight: 600;">${status}</span></td>
                <td>${p.isFeatured ? '<span style="color:#2e6b4d; font-weight:bold;">Yes</span>' : '<span style="color:#999;">No</span>'}</td>
                <td>
                    <button class="btn-secondary" style="padding: 4px 10px; font-size: 0.8rem; width: auto; margin-right: 5px; color: var(--primary-bg); border-color: var(--primary-bg);" onclick="editProduct(${p.id})">Edit</button>
                    <button class="btn-secondary" style="padding: 4px 10px; font-size: 0.8rem; width: auto;" onclick="deleteProduct(${p.id})">Delete</button>
                </td>
            </tr>
        `;
    });
    tbody.innerHTML = html;
}

function showAddProduct() {
    editingProductId = null;
    document.getElementById('formTitle').innerText = 'Add New Product';
    
    // Clear form
    document.getElementById('pName').value = '';
    document.getElementById('pCategory').value = '';
    document.getElementById('pPrice').value = '';
    document.getElementById('pImage').value = '';
    document.getElementById('pAvailabilityStatus').value = 'Available';
    document.getElementById('pIsFeatured').checked = false;
    document.getElementById('pDesc_Desc').value = '';
    document.getElementById('pDesc_Features').value = '';
    document.getElementById('pDesc_Material').value = '';
    document.getElementById('pDesc_Sizes').value = '';
    document.getElementById('pDesc_Fit').value = '';
    document.getElementById('pDesc_Sole').value = '';
    document.getElementById('pDesc_Craft').value = '';
    document.getElementById('pDesc_Care').value = '';
    document.getElementById('pDesc_Warranty').value = '';
    document.getElementById('pDesc_Shipping').value = '';

    document.getElementById('productsList').style.display = 'none';
    document.getElementById('addProductForm').style.display = 'block';
}

function hideAddProduct() {
    editingProductId = null;
    document.getElementById('addProductForm').style.display = 'none';
    document.getElementById('productsList').style.display = 'block';
}

async function editProduct(id) {
    try {
        const res = await fetch(`/api/products/${id}`);
        if (res.ok) {
            const product = await res.json();
            
            editingProductId = product.id;
            document.getElementById('formTitle').innerText = 'Edit Product';
            
            document.getElementById('pName').value = product.name;
            
            // Set category select value by categoryId or matching name fallback
            if (product.categoryId) {
                document.getElementById('pCategory').value = product.categoryId;
            } else {
                // Fallback matching category name
                const catObj = adminAllCategories.find(c => c.name.toLowerCase() === (product.category || '').toLowerCase());
                document.getElementById('pCategory').value = catObj ? catObj.id : '';
            }
            
            document.getElementById('pAvailabilityStatus').value = product.availabilityStatus || 'Available';
            document.getElementById('pIsFeatured').checked = product.isFeatured || false;
            document.getElementById('pPrice').value = product.price;
            document.getElementById('pImage').value = ''; // Don't require re-uploading
            
            document.getElementById('pDesc_Sizes').value = product.sizes ? product.sizes.join(', ') : '';

            // Parse description JSON if possible
            let descObj = {};
            try {
                if (product.description && product.description.startsWith('{')) {
                    descObj = JSON.parse(product.description);
                } else {
                    descObj.description = product.description; // fallback
                }
            } catch(e) {}
            
            document.getElementById('pDesc_Desc').value = descObj.description || '';
            document.getElementById('pDesc_Features').value = descObj.features || '';
            document.getElementById('pDesc_Material').value = descObj.material || '';
            document.getElementById('pDesc_Fit').value = descObj.fit || '';
            document.getElementById('pDesc_Sole').value = descObj.sole || '';
            document.getElementById('pDesc_Craft').value = descObj.craftsmanship || '';
            document.getElementById('pDesc_Care').value = descObj.care || '';
            document.getElementById('pDesc_Warranty').value = descObj.warranty || '';
            document.getElementById('pDesc_Shipping').value = descObj.shipping || '';

            document.getElementById('productsList').style.display = 'none';
            document.getElementById('addProductForm').style.display = 'block';
        }
    } catch (err) {
        console.error("Error fetching product for edit", err);
    }
}

async function saveProduct() {
    const name = document.getElementById('pName').value;
    const catId = parseInt(document.getElementById('pCategory').value) || null;
    const availabilityStatus = document.getElementById('pAvailabilityStatus').value;
    const isFeatured = document.getElementById('pIsFeatured').checked;
    const price = parseFloat(document.getElementById('pPrice').value);
    
    const descObj = {
        description: document.getElementById('pDesc_Desc').value,
        features: document.getElementById('pDesc_Features').value,
        material: document.getElementById('pDesc_Material').value,
        fit: document.getElementById('pDesc_Fit').value,
        sole: document.getElementById('pDesc_Sole').value,
        craftsmanship: document.getElementById('pDesc_Craft').value,
        care: document.getElementById('pDesc_Care').value,
        warranty: document.getElementById('pDesc_Warranty').value,
        shipping: document.getElementById('pDesc_Shipping').value
    };
    
    const descJson = JSON.stringify(descObj);
    const sizesInput = document.getElementById('pDesc_Sizes').value;
    const sizesArray = sizesInput.split(',').map(s => s.trim()).filter(s => s !== '');

    const fileInput = document.getElementById('pImage');
    let base64Image = null;
    
    if (fileInput.files && fileInput.files[0]) {
        const file = fileInput.files[0];
        base64Image = await new Promise((resolve) => {
            const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result);
            reader.readAsDataURL(file);
        });
    } else if (!editingProductId) {
        alert("Please select an image");
        return;
    }

    const payload = {
        name: name,
        categoryId: catId,
        availabilityStatus: availabilityStatus,
        isFeatured: isFeatured,
        price: price,
        description: descJson,
        slug: name.toLowerCase().replace(/ /g, '-'),
        sizes: sizesArray
    };
    
    if (base64Image) {
        payload.thumbnail = base64Image;
        payload.images = [base64Image];
    }

    try {
        const url = editingProductId ? `/api/products/${editingProductId}` : '/api/products';
        const method = editingProductId ? 'PUT' : 'POST';

        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        
        if (res.ok) {
            hideAddProduct();
            fetchProducts();
        } else {
            alert("Failed to save product");
        }
    } catch (err) {
        alert("Error saving product");
    }
}

async function deleteProduct(id) {
    if(confirm('Are you sure you want to delete this product?')) {
        try {
            const res = await fetch(`/api/adminapi/products/${id}`, { method: 'DELETE' });
            if (res.ok) {
                fetchProducts();
            } else {
                alert('Failed to delete product');
            }
        } catch (err) {
            alert('Error deleting product');
        }
    }
}

// Pending Payments Logic
async function fetchPendingPayments() {
    try {
        const token = localStorage.getItem('token');
        const res = await fetch('/api/adminapi/pending-payments', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.ok) {
            const pending = await res.json();
            renderPendingPayments(pending);
        }
    } catch (err) {
        console.error("Error loading pending payments", err);
    }
}

function renderPendingPayments(payments) {
    const tbody = document.getElementById('paymentVerificationTableBody');
    if (!tbody) return;

    if (payments.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="10" style="text-align:center; color:#666; padding:2rem;">No payment requests found.</td>
            </tr>
        `;
        return;
    }

    let html = '';
    payments.forEach(p => {
        const dateStr = new Date(p.date).toLocaleDateString() + ' ' + new Date(p.date).toLocaleTimeString();
        
        let actionHtml = '';
        if (p.status === "Payment Verification Pending") {
            actionHtml = `
                <div style="display:flex; gap:6px;">
                    <button class="btn-primary" style="padding: 4px 10px; font-size: 0.8rem; width: auto; background-color:#2e6b4d; color:#fff;" onclick="verifyPaymentOrder('${p.id}')">Verify</button>
                    <button class="btn-primary" style="padding: 4px 10px; font-size: 0.8rem; width: auto; background-color:#dc3545; color:#fff;" onclick="openRejectModal('${p.id}')">Reject</button>
                </div>
            `;
        } else if (p.status === "Payment Rejected") {
            actionHtml = `
                <span style="color:#721c24; background-color:#f8d7da; padding: 4px 8px; border-radius: 4px; font-size:0.8rem; font-weight:bold; display:inline-block; max-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${p.rejectionReason || 'Invalid details'}">Rejected: ${p.rejectionReason || 'Invalid details'}</span>
            `;
        } else {
            actionHtml = `
                <span style="color:#155724; background-color:#d4edda; padding: 4px 8px; border-radius: 4px; font-size:0.8rem; font-weight:bold;">Approved (${p.status})</span>
            `;
        }

        html += `
            <tr>
                <td style="font-weight: bold;">#${p.id}</td>
                <td>${dateStr}</td>
                <td>${p.customer}</td>
                <td>Rs. ${p.amount.toFixed(2)}</td>
                <td>${p.paymentMethod}</td>
                <td><code>${p.paymentRefId || 'N/A'}</code></td>
                <td>${p.senderName || 'N/A'}</td>
                <td>${p.senderMobile || 'N/A'}</td>
                <td>
                    ${p.paymentScreenshot ? `<a href="${p.paymentScreenshot}" target="_blank" style="text-decoration: underline; color: var(--primary-gold); font-weight:bold;">View Screenshot</a>` : 'N/A'}
                </td>
                <td>
                    ${actionHtml}
                </td>
            </tr>
        `;
    });
    tbody.innerHTML = html;
}

async function verifyPaymentOrder(id) {
    if (!confirm(`Are you sure you want to verify the payment for Order #${id}? This will confirm the order.`)) return;

    const token = localStorage.getItem('token');
    try {
        const res = await fetch(`/api/adminapi/orders/${id}/verify-payment`, {
            method: 'PUT',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.ok) {
            alert('Payment verified and order confirmed!');
            fetchPendingPayments();
            fetchDashboardStats();
            fetchOrders();
        } else {
            const data = await res.json();
            alert(data.message || 'Failed to verify payment');
        }
    } catch (err) {
        alert('Error verifying payment');
    }
}

// Rejection Modal Logic
function openRejectModal(id) {
    document.getElementById('rejectOrderId').value = id;
    document.getElementById('rejectionReasonSelect').value = 'Transaction not received';
    document.getElementById('otherRejectionReason').value = '';
    document.getElementById('otherReasonContainer').style.display = 'none';
    document.getElementById('rejectModal').style.display = 'flex';
}

function closeRejectModal() {
    document.getElementById('rejectModal').style.display = 'none';
}

function onRejectionReasonChange() {
    const sel = document.getElementById('rejectionReasonSelect').value;
    const otherCont = document.getElementById('otherReasonContainer');
    if (sel === 'Other') {
        otherCont.style.display = 'block';
    } else {
        otherCont.style.display = 'none';
    }
}

async function submitRejection() {
    const id = document.getElementById('rejectOrderId').value;
    const sel = document.getElementById('rejectionReasonSelect').value;
    let reason = sel;
    
    if (sel === 'Other') {
        reason = document.getElementById('otherRejectionReason').value.trim();
        if (!reason) {
            alert('Please specify the custom rejection reason.');
            return;
        }
    }

    const token = localStorage.getItem('token');
    try {
        const res = await fetch(`/api/adminapi/orders/${id}/reject-payment`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ rejectionReason: reason })
        });
        if (res.ok) {
            alert('Payment rejected. Customer will be notified.');
            closeRejectModal();
            fetchPendingPayments();
            fetchDashboardStats();
            fetchOrders();
        } else {
            const data = await res.json();
            alert(data.message || 'Failed to reject payment');
        }
    } catch (err) {
        alert('Error rejecting payment');
    }
}

// Manual Payment Settings Management
let manualPaymentSettings = [];

async function fetchManualPaymentSettings() {
    try {
        const token = localStorage.getItem('token');
        const res = await fetch('/api/adminapi/manual-payment-settings', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.ok) {
            manualPaymentSettings = await res.json();
            renderPaymentSettings();
        }
    } catch (err) {
        console.error("Error loading payment settings", err);
    }
}

function renderPaymentSettings() {
    const container = document.getElementById('paymentSettingsContainer');
    if (!container) return;
    
    let html = '<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem;">';
    manualPaymentSettings.forEach(s => {
        html += `
            <div style="border: 1px solid #ddd; padding: 1.5rem; border-radius: 8px; background: #fafafa; color: #333; display: flex; flex-direction: column;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; border-bottom: 1px dashed #ccc; padding-bottom: 0.5rem;">
                    <h4 style="margin:0; font-family:var(--font-body); font-weight:600; color:var(--primary-bg); font-size:1.1rem;">${s.methodName}</h4>
                    <label style="display: flex; align-items: center; gap: 8px; font-weight: 600; cursor: pointer;">
                        <input type="checkbox" id="enable_${s.id}" ${s.isEnabled ? 'checked' : ''} style="transform: scale(1.15);"> Enable
                    </label>
                </div>
                <input type="hidden" id="method_id_${s.id}" value="${s.id}">
        `;
        
        if (s.methodName === "Bank Transfer") {
            html += `
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">Bank Name</label>
                    <input type="text" id="bank_name_${s.id}" class="form-control" value="${s.bankName || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">Account Title</label>
                    <input type="text" id="acc_title_${s.id}" class="form-control" value="${s.accountTitle || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">Account Number</label>
                    <input type="text" id="acc_num_${s.id}" class="form-control" value="${s.accountNumber || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">IBAN</label>
                    <input type="text" id="iban_${s.id}" class="form-control" value="${s.iban || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
            `;
        } else if (s.methodName === "JazzCash" || s.methodName === "Easypaisa") {
            html += `
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">Account Title</label>
                    <input type="text" id="acc_title_${s.id}" class="form-control" value="${s.accountTitle || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">Mobile Number</label>
                    <input type="text" id="mobile_${s.id}" class="form-control" value="${s.mobileNumber || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
            `;
        } else if (s.methodName === "Raast ID") {
            html += `
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">Account Title</label>
                    <input type="text" id="acc_title_${s.id}" class="form-control" value="${s.accountTitle || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
                <div class="form-group" style="margin-bottom: 1rem;">
                    <label style="color:#555; margin-bottom: 4px;">Raast ID</label>
                    <input type="text" id="raast_${s.id}" class="form-control" value="${s.raastId || ''}" style="color:#333; background:#fff; border:1px solid #ccc; padding: 10px;">
                </div>
            `;
        }
        
        html += `</div>`;
    });
    html += '</div>';
    container.innerHTML = html;
}

async function savePaymentSettings() {
    const payload = [];
    const msg = document.getElementById('paymentSettingsMsg');
    
    manualPaymentSettings.forEach(s => {
        const item = {
            id: s.id,
            methodName: s.methodName,
            isEnabled: document.getElementById(`enable_${s.id}`).checked,
            accountTitle: document.getElementById(`acc_title_${s.id}`) ? document.getElementById(`acc_title_${s.id}`).value.trim() : null
        };
        
        if (s.methodName === "Bank Transfer") {
            item.bankName = document.getElementById(`bank_name_${s.id}`).value.trim();
            item.accountNumber = document.getElementById(`acc_num_${s.id}`).value.trim();
            item.iban = document.getElementById(`iban_${s.id}`).value.trim();
        } else if (s.methodName === "JazzCash" || s.methodName === "Easypaisa") {
            item.mobileNumber = document.getElementById(`mobile_${s.id}`).value.trim();
        } else if (s.methodName === "Raast ID") {
            item.raastId = document.getElementById(`raast_${s.id}`).value.trim();
        }
        
        payload.push(item);
    });
    
    try {
        const token = localStorage.getItem('token');
        const res = await fetch('/api/adminapi/manual-payment-settings', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });
        
        if (res.ok) {
            msg.style.display = 'block';
            msg.style.color = 'var(--success)';
            msg.innerText = 'Payment settings updated successfully.';
            setTimeout(() => { msg.style.display = 'none'; }, 3000);
            fetchManualPaymentSettings();
        } else {
            msg.style.display = 'block';
            msg.style.color = 'var(--error)';
            msg.innerText = 'Failed to update payment settings.';
        }
    } catch (err) {
        console.error(err);
        msg.style.display = 'block';
        msg.style.color = 'var(--error)';
        msg.innerText = 'Network error.';
    }
}

// Categories CRUD Management
let adminAllCategories = [];

async function fetchCategoriesList() {
    try {
        const token = localStorage.getItem('token');
        const res = await fetch('/api/adminapi/categories', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.ok) {
            adminAllCategories = await res.json() || [];
            
            // Populate category select element in product add/edit form
            const select = document.getElementById('pCategory');
            if (select) {
                const currentVal = select.value;
                select.innerHTML = '<option value="">Select Category</option>';
                adminAllCategories.forEach(c => {
                    if (c.isActive) {
                        const opt = document.createElement('option');
                        opt.value = c.id;
                        opt.textContent = c.name;
                        select.appendChild(opt);
                    }
                });
                if (currentVal) {
                    select.value = currentVal;
                }
            }
            
            renderAdminCategoriesTable();
        }
    } catch (err) {
        console.error("Error loading categories:", err);
    }
}

function renderAdminCategoriesTable() {
    const tbody = document.getElementById('adminCategoriesTableBody');
    if (!tbody) return;

    if (adminAllCategories.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="5" style="text-align:center; color:#666; padding:2rem;">No categories found.</td>
            </tr>
        `;
        return;
    }

    let html = '';
    adminAllCategories.forEach(c => {
        const statusBadge = c.isActive 
            ? `<span style="background-color: #2e6b4d; color: #fff; padding: 2px 8px; border-radius: 4px; font-size: 0.8rem; font-weight: 600;">Active</span>`
            : `<span style="background-color: #dc3545; color: #fff; padding: 2px 8px; border-radius: 4px; font-size: 0.8rem; font-weight: 600;">Inactive</span>`;
        
        html += `
            <tr>
                <td style="font-weight: bold;">#${c.id}</td>
                <td>${c.name}</td>
                <td>${statusBadge}</td>
                <td>${c.displayOrder}</td>
                <td>
                    <div style="display:flex; gap:6px;">
                        <button class="btn-secondary" style="padding: 4px 10px; font-size: 0.8rem; width: auto; color: var(--primary-bg); border-color: var(--primary-bg);" onclick="openEditCategoryModal(${c.id})">Edit</button>
                        <button class="btn-secondary" style="padding: 4px 10px; font-size: 0.8rem; width: auto;" onclick="deleteCategory(${c.id})">Delete</button>
                    </div>
                </td>
            </tr>
        `;
    });
    tbody.innerHTML = html;
}

function openAddCategoryModal() {
    document.getElementById('editCategoryId').value = '';
    document.getElementById('categoryNameInput').value = '';
    document.getElementById('categoryOrderInput').value = '0';
    document.getElementById('categoryActiveInput').checked = true;
    document.getElementById('categoryModalTitle').innerText = 'Add Category';
    document.getElementById('categoryModal').style.display = 'flex';
}

function openEditCategoryModal(id) {
    const cat = adminAllCategories.find(c => c.id === id);
    if (!cat) return;

    document.getElementById('editCategoryId').value = cat.id;
    document.getElementById('categoryNameInput').value = cat.name;
    document.getElementById('categoryOrderInput').value = cat.displayOrder;
    document.getElementById('categoryActiveInput').checked = cat.isActive;
    document.getElementById('categoryModalTitle').innerText = 'Edit Category';
    document.getElementById('categoryModal').style.display = 'flex';
}

function closeCategoryModal() {
    document.getElementById('categoryModal').style.display = 'none';
}

async function saveCategory() {
    const id = document.getElementById('editCategoryId').value;
    const name = document.getElementById('categoryNameInput').value.trim();
    const order = parseInt(document.getElementById('categoryOrderInput').value) || 0;
    const isActive = document.getElementById('categoryActiveInput').checked;

    if (!name) {
        alert("Please enter a category name.");
        return;
    }

    const payload = {
        name: name,
        displayOrder: order,
        isActive: isActive
    };

    const token = localStorage.getItem('token');
    const method = id ? 'PUT' : 'POST';
    const url = id ? `/api/adminapi/categories/${id}` : '/api/adminapi/categories';

    try {
        const res = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            closeCategoryModal();
            fetchCategoriesList();
            fetchProducts(); // refresh products list to propagate name changes
            
            const msg = document.getElementById('categoryMsg');
            if (msg) {
                msg.style.display = 'block';
                msg.style.color = 'var(--success)';
                msg.innerText = id ? 'Category updated successfully.' : 'Category created successfully.';
                setTimeout(() => { msg.style.display = 'none'; }, 3000);
            }
        } else {
            const data = await res.json();
            alert(data.message || 'Failed to save category.');
        }
    } catch (err) {
        console.error(err);
        alert('Error saving category.');
    }
}

async function deleteCategory(id) {
    if (!confirm('Are you sure you want to delete this category? This cannot be undone.')) return;

    const token = localStorage.getItem('token');
    try {
        const res = await fetch(`/api/adminapi/categories/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (res.ok) {
            fetchCategoriesList();
            const msg = document.getElementById('categoryMsg');
            if (msg) {
                msg.style.display = 'block';
                msg.style.color = 'var(--success)';
                msg.innerText = 'Category deleted successfully.';
                setTimeout(() => { msg.style.display = 'none'; }, 3000);
            }
        } else {
            const data = await res.json();
            alert(data.message || 'Failed to delete category.');
        }
    } catch (err) {
        console.error(err);
        alert('Error deleting category.');
    }
}
