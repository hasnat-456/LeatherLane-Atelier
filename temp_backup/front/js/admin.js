// Initialization
let editingProductId = null;

document.addEventListener('DOMContentLoaded', () => {
    fetchDashboardStats();
    fetchOrders();
    fetchProducts();
});

// Tab Switching
function switchTab(tabId) {
    // Update nav items
    document.querySelectorAll('.nav-item').forEach(item => item.classList.remove('active'));
    event.currentTarget.classList.add('active');
    
    // Update sections
    document.querySelectorAll('.section').forEach(sec => sec.classList.remove('active'));
    document.getElementById(tabId).classList.add('active');
}

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
                    <select class="status-select" onchange="updateOrderStatus('${o.id}', this.value)">
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
        html += `
            <tr>
                <td><img src="${p.thumbnail || 'https://via.placeholder.com/50'}" alt="${p.name}" style="width: 50px; height: 50px; object-fit: cover; border-radius: 4px;"></td>
                <td style="font-weight: 500;">${p.name}</td>
                <td>${p.category}</td>
                <td>Rs. ${p.price.toFixed(2)}</td>
                <td>${p.stock}</td>
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
    document.getElementById('pPrice').value = '';
    document.getElementById('pImage').value = '';
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
            document.getElementById('pCategory').value = product.category;
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
    const cat = document.getElementById('pCategory').value;
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
    const sizesArray = sizesInput ? sizesInput.split(',').map(s => s.trim()).filter(s => s !== '') : [];

    const fileInput = document.getElementById('pImage');
    let base64Image = null;
    
    if (fileInput.files && fileInput.files.length > 0) {
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
        category: cat,
        price: price,
        description: descJson,
        stock: 50,
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
