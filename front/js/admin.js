
let aboutUsQuill = null;
function initAboutUsQuill() {
    if (!aboutUsQuill && typeof Quill !== 'undefined' && document.getElementById('aboutUsEditor')) {
        aboutUsQuill = new Quill('#aboutUsEditor', {
            theme: 'snow',
            modules: {
                toolbar: [
                    [{ 'header': [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    ['blockquote', 'code-block'],
                    [{ 'list': 'ordered'}, { 'list': 'bullet' }],
                    [{ 'align': [] }],
                    ['link', 'image', 'video'],
                    ['clean']
                ]
            }
        });
    }
}

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
        fetchStoreSettings();
    loadSliderSettings();
    } else if (tabId === 'categories') {
        fetchCategoriesList();
    } else if (tabId === 'dashboard') {
        fetchDashboardStats();
    } else if (tabId === 'orders') {
        fetchOrders();
    } else if (tabId === 'products') {
        fetchProducts();
    } else if (tabId === 'stories') {
        fetchStories();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    initAboutUsQuill();
    fetchDashboardStats();
    fetchOrders();
    fetchProducts();
    fetchPendingPayments();
    fetchManualPaymentSettings();
    fetchCategoriesList();
    fetchStories();

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
    initAboutUsQuill();
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
                <span style="color:#721c24; background-color:#f8d7da; padding: 4px 8px; border-radius: 4px; font-size:0.8rem; font-weight:bold; display:inline-block;" title="${p.rejectionReason || 'Invalid details'}">Rejected: ${p.rejectionReason || 'Invalid details'}</span>
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
    
    let html = '';
    manualPaymentSettings.forEach(s => {
        html += `
            <div style="border: 1px solid #ddd; padding: 1.5rem; border-radius: 8px; margin-bottom: 1.5rem; background: #fafafa; color: #333;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; border-bottom: 1px dashed #ccc; padding-bottom: 0.5rem;">
                    <h4 style="margin:0; font-family:var(--font-body); font-weight:600; color:var(--primary-bg); font-size:1.1rem;">${s.methodName}</h4>
                    <label style="display: flex; align-items: center; gap: 8px; font-weight: 600; cursor: pointer;">
                        <input type="checkbox" id="enable_${s.id}" ${s.isEnabled ? 'checked' : ''} style="transform: scale(1.15);"> Enable Method
                    </label>
                </div>
                <input type="hidden" id="method_id_${s.id}" value="${s.id}">
        `;
        
        if (s.methodName === "Bank Transfer") {
            html += `
                <div class="grid-2">
                    <div class="form-group">
                        <label style="color:#555;">Bank Name</label>
                        <input type="text" id="bank_name_${s.id}" class="form-control" value="${s.bankName || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                    <div class="form-group">
                        <label style="color:#555;">Account Title</label>
                        <input type="text" id="acc_title_${s.id}" class="form-control" value="${s.accountTitle || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                </div>
                <div class="grid-2">
                    <div class="form-group">
                        <label style="color:#555;">Account Number</label>
                        <input type="text" id="acc_num_${s.id}" class="form-control" value="${s.accountNumber || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                    <div class="form-group">
                        <label style="color:#555;">IBAN</label>
                        <input type="text" id="iban_${s.id}" class="form-control" value="${s.iban || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                </div>
            `;
        } else if (s.methodName === "JazzCash" || s.methodName === "Easypaisa") {
            html += `
                <div class="grid-2">
                    <div class="form-group">
                        <label style="color:#555;">Account Title</label>
                        <input type="text" id="acc_title_${s.id}" class="form-control" value="${s.accountTitle || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                    <div class="form-group">
                        <label style="color:#555;">Mobile Number</label>
                        <input type="text" id="mobile_${s.id}" class="form-control" value="${s.mobileNumber || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                </div>
            `;
        } else if (s.methodName === "Raast ID") {
            html += `
                <div class="grid-2">
                    <div class="form-group">
                        <label style="color:#555;">Account Title</label>
                        <input type="text" id="acc_title_${s.id}" class="form-control" value="${s.accountTitle || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                    <div class="form-group">
                        <label style="color:#555;">Raast ID</label>
                        <input type="text" id="raast_${s.id}" class="form-control" value="${s.raastId || ''}" style="color:#333; background:#fff; border:1px solid #ccc;">
                    </div>
                </div>
            `;
        }
        
        html += `</div>`;
    });
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
// DEALS MANAGEMENT
function toggleDealScope() {
    const scope = document.getElementById('deal-scope').value;
    document.getElementById('deal-product-group').style.display = (scope === 'Product') ? 'block' : 'none';
    document.getElementById('deal-category-group').style.display = (scope === 'Category') ? 'block' : 'none';
}

async function loadDealsFormOptions() {
    try {
        const prodRes = await fetch('/api/products');
        const products = await prodRes.json();
        const prodSelect = document.getElementById('deal-product');
        prodSelect.innerHTML = products.map(p => `<option value="${p.id}">${p.name}</option>`).join('');
        
        const catRes = await fetch('/api/products/categories', { headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }});
        const categories = await catRes.json();
        const catSelect = document.getElementById('deal-category');
        catSelect.innerHTML = categories.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
    } catch (err) {
        console.error('Error loading deal options', err);
    }
}

async function loadDeals() {
    try {
        const res = await fetch('/api/deals/adminlist', { headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }});
        if (res.ok) {
            const deals = await res.json();
            const tbody = document.getElementById('admin-deals-table-body');
            tbody.innerHTML = '';
            
            if(deals.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" style="text-align: center;">No deals found</td></tr>';
                return;
            }
            
            deals.forEach(d => {
                const target = d.scope === 'Product' ? 'Product #' + d.productId : (d.scope === 'Category' ? 'Category #' + d.categoryId : 'All Products');
                const discount = d.discountType === 'Percentage' ? d.discountValue + '%' : d.discountValue + ' PKR';
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td>${d.scope}</td>
                    <td>${target}</td>
                    <td>${discount}</td>
                    <td>${new Date(d.startTime).toLocaleString()}</td>
                    <td>${new Date(d.endTime).toLocaleString()}</td>
                    <td>
                        <button onclick="deleteDeal(${d.id})" class="btn" style="background-color: #dc3545; color: white; border: none; padding: 6px 12px; border-radius: 4px; cursor: pointer;">Delete</button>
                    </td>
                `;
                tbody.appendChild(row);
            });
        }
    } catch (err) {
        console.error('Error loading deals', err);
    }
}

async function createDeal(e) {
    e.preventDefault();
    const scope = document.getElementById('deal-scope').value;
    const deal = {
        scope: scope,
        discountType: document.getElementById('deal-discount-type').value,
        discountValue: parseFloat(document.getElementById('deal-discount-value').value),
        startTime: document.getElementById('deal-start').value,
        endTime: document.getElementById('deal-end').value
    };
    
    if(scope === 'Product') deal.productId = parseInt(document.getElementById('deal-product').value);
    if(scope === 'Category') deal.categoryId = parseInt(document.getElementById('deal-category').value);
    
    try {
        const res = await fetch('/api/deals', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + localStorage.getItem('token') },
            body: JSON.stringify(deal)
        });
        if(res.ok) {
            alert('Deal created successfully!');
            document.getElementById('create-deal-form').reset();
            loadDeals();
        } else {
            alert('Failed to create deal.');
        }
    } catch (err) {
        alert('Error: ' + err.message);
    }
}

async function deleteDeal(id) {
    if(!confirm('Are you sure you want to delete this deal?')) return;
    try {
        const res = await fetch('/api/deals/' + id, {
            method: 'DELETE',
            headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
        });
        if(res.ok) {
            loadDeals();
        } else {
            alert('Failed to delete deal.');
        }
    } catch (err) {
        alert('Error: ' + err.message);
    }
}

// Hook into existing switchTab
const originalSwitchTab = window.switchTab;
window.switchTab = function(tabId) {
    if (originalSwitchTab) {
        originalSwitchTab(tabId);
    } else {
        document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
        document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
        
        const section = document.getElementById(tabId);
        if(section) section.classList.add('active');
        
        const nav = document.querySelector(`.nav-item[onclick="switchTab('${tabId}')"]`);
        if(nav) nav.classList.add('active');
    }
    
    if(tabId === 'deals') {
        loadDealsFormOptions();
        loadDeals();
    }
};

// --- Store Settings ---
async function fetchStoreSettings() {
    try {
        const res = await fetch('/api/settings');
        if (res.ok) {
            const data = await res.json();
            document.getElementById('siteAddress').value = data.address || '';
            document.getElementById('siteEmail').value = data.email || '';
            document.getElementById('sitePhone').value = data.phone || '';
            document.getElementById('siteHours').value = data.businessHours || '';
            document.getElementById('siteFb').value = data.facebookUrl || '';
            document.getElementById('siteIg').value = data.instagramUrl || '';
            document.getElementById('siteWa').value = data.whatsAppUrl || '';
            document.getElementById('siteTk').value = data.tikTokUrl || '';
        }
    } catch (e) {
        console.error('Error fetching settings', e);
    }
}

async function saveStoreSettings() {
    const btn = document.getElementById('saveStoreBtn');
    btn.innerText = 'Saving...';
    
    const payload = {
        address: document.getElementById('siteAddress').value,
        email: document.getElementById('siteEmail').value,
        phone: document.getElementById('sitePhone').value,
        businessHours: document.getElementById('siteHours').value,
        facebookUrl: document.getElementById('siteFb').value,
        instagramUrl: document.getElementById('siteIg').value,
        whatsAppUrl: document.getElementById('siteWa').value,
        tikTokUrl: document.getElementById('siteTk').value
    };
    
    try {
        const token = localStorage.getItem('token');
        const res = await fetch('/api/settings', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });
        
        if (res.ok) {
            alert('Settings saved successfully!');
        } else {
            alert('Failed to save settings.');
        }
    } catch (e) {
        console.error(e);
        alert('Error saving settings.');
    } finally {
        btn.innerText = 'Save Settings';
    }
}


// --- Stories Management ---

let quillEditor = null;

function initQuill() {
    if (!quillEditor && typeof Quill !== 'undefined') {
        quillEditor = new Quill('#storyContentEditor', {
            theme: 'snow',
            modules: {
                toolbar: [
                    [{ 'header': [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    ['blockquote', 'code-block'],
                    [{ 'list': 'ordered'}, { 'list': 'bullet' }],
                    [{ 'align': [] }],
                    ['link', 'image', 'video'],
                    ['clean']
                ]
            }
        });
    }
}

async function fetchStories() {
    try {
        const res = await fetch('/api/blogs');
        if (!res.ok) throw new Error('Failed to fetch stories');
        const data = await res.json();
        
        const tbody = document.getElementById('storiesTableBody');
        if (!tbody) return;
        tbody.innerHTML = '';
        
        data.forEach(story => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${story.id}</td>
                <td>${story.title}</td>
                <td>${story.category || ''}</td>
                <td>${new Date(story.createdAt).toLocaleDateString()}</td>
                <td>
                    <button class="btn" style="padding:4px 8px; font-size:0.8rem;" onclick="editStory(${story.id})">Edit</button>
                    <button class="btn" style="padding:4px 8px; font-size:0.8rem; background:#dc3545;" onclick="deleteStory(${story.id})">Delete</button>
                </td>
            `;
            tbody.appendChild(tr);
        });
    } catch (error) {
        console.error(error);
    }
}

function openStoryModal() {
    initQuill();
    document.getElementById('storyModalTitle').textContent = 'Add Story';
    document.getElementById('storyId').value = '';
    document.getElementById('storyTitle').value = '';
    document.getElementById('storyCategory').value = '';
    document.getElementById('storyCoverImage').value = '';
    document.getElementById('storyExcerpt').value = '';
    if (quillEditor) quillEditor.root.innerHTML = '';
    document.getElementById('storyModal').style.display = 'flex';
}

function closeStoryModal() {
    document.getElementById('storyModal').style.display = 'none';
}

async function editStory(id) {
    try {
        const res = await fetch(`/api/blogs/${id}`);
        if (!res.ok) throw new Error('Failed to fetch story details');
        const story = await res.json();
        
        initQuill();
        document.getElementById('storyModalTitle').textContent = 'Edit Story';
        document.getElementById('storyId').value = story.id;
        document.getElementById('storyTitle').value = story.title;
        document.getElementById('storyCategory').value = story.category;
        
        document.getElementById('storyCoverImage').value = story.image || '';
        const preview = document.getElementById('storyCoverPreview');
        if (preview) {
            if (story.image) {
                preview.src = story.image;
                preview.style.display = 'block';
            } else {
                preview.style.display = 'none';
            }
        }

        document.getElementById('storyExcerpt').value = story.excerpt || '';
        if (quillEditor) quillEditor.root.innerHTML = story.content || '';
        
        document.getElementById('storyModal').style.display = 'flex';
    } catch (error) {
        console.error(error);
        alert('Failed to load story');
    }
}

async function saveStory() {
    const id = document.getElementById('storyId').value;
    const title = document.getElementById('storyTitle').value;
    const category = document.getElementById('storyCategory').value;
    
    const fileInput = document.getElementById('storyCoverImageFile');
    let image = document.getElementById('storyCoverImage').value;
    
    if (fileInput && fileInput.files.length > 0) {
        const formData = new FormData();
        formData.append('imageFile', fileInput.files[0]);
        
        try {
            const uploadRes = await fetch('/api/blogs/upload-image', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
                body: formData
            });
            if (uploadRes.ok) {
                const uploadData = await uploadRes.json();
                image = uploadData.url;
            } else {
                alert('Image upload failed');
                return;
            }
        } catch(e) {
            console.error(e);
            alert('Error uploading image');
            return;
        }
    }

    const excerpt = document.getElementById('storyExcerpt').value;
    const content = quillEditor ? quillEditor.root.innerHTML : '';
    
    if (!title) {
        alert('Title is required');
        return;
    }
    
    const payload = {
        title,
        category,
        image,
        excerpt,
        content
    };
    
    const url = id ? `/api/blogs/${id}` : '/api/blogs';
    const method = id ? 'PUT' : 'POST';
    const token = localStorage.getItem('token');
    
    try {
        const res = await fetch(url, {
            method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });
        
        if (!res.ok) throw new Error('Failed to save story');
        closeStoryModal();
        fetchStories();
    } catch (error) {
        console.error(error);
        alert('Error saving story');
    }
}

async function deleteStory(id) {
    if (!confirm('Are you sure you want to delete this story?')) return;
    const token = localStorage.getItem('token');
    try {
        const res = await fetch(`/api/blogs/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        if (!res.ok) throw new Error('Failed to delete story');
        fetchStories();
    } catch (error) {
        console.error(error);
        alert('Error deleting story');
    }
}


// Size Guide Table Logic
function renderSizeGuideTable(dataStr) {
    const tbody = document.querySelector('#sizeGuideTable tbody');
    if (!tbody) return;
    tbody.innerHTML = '';
    
    let data = [];
    try {
        if (dataStr) data = JSON.parse(dataStr);
    } catch(e) {}
    
    data.forEach(row => {
        tbody.insertAdjacentHTML('beforeend', createSizeGuideRowHTML(row.pk, row.uk, row.us, row.eu, row.cm));
    });
}

function createSizeGuideRowHTML(pk='', uk='', us='', eu='', cm='') {
    return `
        <tr>
            <td><input type="text" value="${pk}" style="width:100%; padding:5px;"></td>
            <td><input type="text" value="${uk}" style="width:100%; padding:5px;"></td>
            <td><input type="text" value="${us}" style="width:100%; padding:5px;"></td>
            <td><input type="text" value="${eu}" style="width:100%; padding:5px;"></td>
            <td><input type="text" value="${cm}" style="width:100%; padding:5px;"></td>
            <td><button type="button" class="btn" style="padding:5px 10px; background:#dc3545; color:#fff;" onclick="this.closest('tr').remove()">Delete</button></td>
        </tr>
    `;
}

function addSizeGuideRow() {
    const tbody = document.querySelector('#sizeGuideTable tbody');
    if (!tbody) return;
    tbody.insertAdjacentHTML('beforeend', createSizeGuideRowHTML());
}

function getSizeGuideData() {
    const tbody = document.querySelector('#sizeGuideTable tbody');
    if (!tbody) return "[]";
    
    const rows = Array.from(tbody.querySelectorAll('tr'));
    const data = rows.map(tr => {
        const inputs = tr.querySelectorAll('input');
        return {
            pk: inputs[0].value,
            uk: inputs[1].value,
            us: inputs[2].value,
            eu: inputs[3].value,
            cm: inputs[4].value
        };
    });
    return JSON.stringify(data);
}


// Newsletter Admin Logic
function toggleAllSubs(checkbox) {
    const checkboxes = document.querySelectorAll('.sub-checkbox');
    checkboxes.forEach(cb => cb.checked = checkbox.checked);
}

function updateBulkBtnText() {
    const btn = document.getElementById('bulkSendBtn');
    const target = document.querySelector('input[name="emailTarget"]:checked').value;
    if (target === 'selected') {
        btn.textContent = 'Send to Selected Subscribers';
    } else {
        btn.textContent = 'Send to All Subscribers';
    }
}

async function loadSubscribers() {
    try {
        const token = localStorage.getItem('adminToken');
        const res = await fetch('/api/newsletter/subscribers', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (!res.ok) throw new Error('Failed to load subscribers');
        const subs = await res.json();
        
        const tbody = document.getElementById('subscribersTableBody');
        if (tbody) {
            tbody.innerHTML = subs.map(s => `
                <tr>
                    <td><input type="checkbox" class="sub-checkbox" value="${s.email || s.Email}"></td>
                    <td>${s.email || s.Email}</td>
                    <td>${new Date(s.subscribedAt || s.SubscribedAt).toLocaleString()}</td>
                </tr>
            `).join('');
        }
    } catch (err) {
        console.error(err);
    }
}

async function sendBulkEmail(e) {
    e.preventDefault();
    
    const target = document.querySelector('input[name="emailTarget"]:checked').value;
    let selectedEmails = [];
    
    if (target === 'selected') {
        const checkboxes = document.querySelectorAll('.sub-checkbox:checked');
        selectedEmails = Array.from(checkboxes).map(cb => cb.value);
        
        if (selectedEmails.length === 0) {
            alert('Please select at least one subscriber to email.');
            return;
        }
    }

    const btn = document.getElementById('bulkSendBtn');
    const msg = document.getElementById('bulkEmailMsg');
    const subject = document.getElementById('bulkSubject').value;
    const body = document.getElementById('bulkBody').value;
    
    btn.disabled = true;
    btn.textContent = 'Sending...';
    msg.style.display = 'none';
    
    try {
        const token = localStorage.getItem('adminToken');
        const res = await fetch('/api/newsletter/send-bulk', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}` 
            },
            body: JSON.stringify({ subject, body, emails: selectedEmails })
        });
        
        const data = await res.json();
        
        msg.style.display = 'block';
        if (res.ok) {
            msg.style.color = '#28a745';
            msg.style.backgroundColor = 'rgba(40, 167, 69, 0.1)';
            msg.textContent = data.message || 'Emails sent successfully!';
            document.getElementById('bulkSubject').value = '';
            document.getElementById('bulkBody').value = '';
        } else {
            msg.style.color = '#dc3545';
            msg.style.backgroundColor = 'rgba(220, 53, 69, 0.1)';
            msg.textContent = data.message || 'Failed to send emails.';
        }
    } catch (err) {
        console.error(err);
        msg.style.display = 'block';
        msg.style.color = '#dc3545';
        msg.style.backgroundColor = 'rgba(220, 53, 69, 0.1)';
        msg.textContent = 'Network error. Try again.';
    } finally {
        btn.disabled = false;
        updateBulkBtnText();
    }
}

// Hook into existing switchTab for newsletter
const originalSwitchTabNewsletter = window.switchTab;
if (typeof originalSwitchTabNewsletter === 'function') {
    window.switchTab = function(tabId) {
        originalSwitchTabNewsletter(tabId);
        if (tabId === 'newsletter') {
            loadSubscribers();
        }
    };
} else {
    console.warn("originalSwitchTabNewsletter not found");
}

// If already on newsletter tab on load
if (document.getElementById('newsletter') && document.getElementById('newsletter').classList.contains('active')) {
    loadSubscribers();
}


// --- Slider Management ---
let heroSliderImages = [];
let craftSliderImages = [];

async function loadSliderSettings() {
    try {
        const res = await fetch('/api/settings');
        if (res.ok) {
            const data = await res.json();
            const parsedHero = data.heroSliderImages ? JSON.parse(data.heroSliderImages) : [];
            const parsedCraft = data.craftSliderImages ? JSON.parse(data.craftSliderImages) : [];
            heroSliderImages = parsedHero || [];
            craftSliderImages = parsedCraft || [];
            renderSliders();
        }
    } catch (e) {
        console.error('Error loading slider settings', e);
    }
}

function renderSliders() {
    const heroContainer = document.getElementById('heroSliderContainer');
    const craftContainer = document.getElementById('craftSliderContainer');
    if(!heroContainer || !craftContainer) return;
    
    heroContainer.innerHTML = '';
    (heroSliderImages || []).forEach((img, i) => {
        heroContainer.innerHTML += `<div style="display:flex; justify-content:space-between; align-items:center; background:#f9f9f9; padding:5px; border:1px solid #ddd;">
                <span style="font-size:0.85rem; word-break:break-all;">${img}</span>
                <button type="button" onclick="removeSliderImage('Hero', ${i})" style="color:red; background:none; border:none; cursor:pointer;">✖</button>
            </div>`;
    });
    
    craftContainer.innerHTML = '';
    (craftSliderImages || []).forEach((img, i) => {
        craftContainer.innerHTML += `<div style="display:flex; justify-content:space-between; align-items:center; background:#f9f9f9; padding:5px; border:1px solid #ddd;">
                <span style="font-size:0.85rem; word-break:break-all;">${img}</span>
                <button type="button" onclick="removeSliderImage('Craft', ${i})" style="color:red; background:none; border:none; cursor:pointer;">✖</button>
            </div>`;
    });
}

function removeSliderImage(type, index) {
    if(type === 'Hero') heroSliderImages.splice(index, 1);
    if(type === 'Craft') craftSliderImages.splice(index, 1);
    renderSliders();
}

async function uploadSliderImage(type) {
    const fileInput = document.getElementById(type === 'Hero' ? 'heroSliderFile' : 'craftSliderFile');
    const files = fileInput.files;
    if(!files || files.length === 0) {
        alert("Please select a file first.");
        return;
    }
    
    if (!heroSliderImages) heroSliderImages = [];
    if (!craftSliderImages) craftSliderImages = [];
    
    const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
    let successCount = 0;
    
    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        const formData = new FormData();
        formData.append('imageFile', file);
        
        try {
            const res = await fetch('/api/adminapi/slider-image', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                body: formData
            });
            
            if (res.ok) {
                const data = await res.json();
                if(type === 'Hero') heroSliderImages.push(data.url);
                if(type === 'Craft') craftSliderImages.push(data.url);
                successCount++;
            } else {
                const err = await res.text();
                console.error("Upload failed for " + file.name + ": " + err);
            }
        } catch (e) {
            console.error("Error uploading image " + file.name, e);
        }
    }
    
    fileInput.value = '';
    renderSliders();
    
    if (successCount > 0) {
        alert(`${successCount} image(s) uploaded successfully! Don't forget to click 'Save Sliders'.`);
    } else {
        alert("Upload failed. Check console for details.");
    }
}

async function saveSliderSettings() {
    const btn = document.getElementById('saveSlidersBtn');
    btn.innerText = 'Saving...';
    
    try {
        const res1 = await fetch('/api/settings');
        if(!res1.ok) throw new Error("Could not fetch current settings");
        const currentData = await res1.json();
        
        currentData.heroSliderImages = JSON.stringify(heroSliderImages || []);
        currentData.craftSliderImages = JSON.stringify(craftSliderImages || []);
        
        const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
        const res2 = await fetch('/api/settings', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(currentData)
        });
        
        if(res2.ok) {
            alert('Sliders saved successfully!');
        } else {
            alert('Failed to save sliders.');
        }
    } catch (e) {
        console.error(e);
        alert('Error saving sliders.');
    } finally {
        btn.innerText = 'Save Sliders';
    }
}
