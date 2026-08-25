const fs = require('fs');

// 1. BACKEND UPDATE (AdminApiController.cs)
let adminApi = fs.readFileSync('back/Controllers/AdminApiController.cs', 'utf8');

const broadcastEndpoint = `
        [HttpPost("broadcast")]
        public async Task<IActionResult> SendBroadcast([FromBody] BroadcastRequest req)
        {
            var users = await _context.Users.ToListAsync();
            
            if (req.SendInApp)
            {
                var now = DateTime.UtcNow;
                var notifs = users.Select(u => new Notification
                {
                    Title = req.Title,
                    Message = req.Message,
                    ActionUrl = req.ActionUrl,
                    UserId = u.Id
                }).ToList();
                
                _context.Notifications.AddRange(notifs);
                await _context.SaveChangesAsync();
            }
            
            if (req.SendEmail)
            {
                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var u in users)
                        {
                            try {
                                await emailSvc.SendEmailAsync(u.Email, req.Title, req.Message);
                                await Task.Delay(500); // Small delay to prevent SMTP throttling
                            } catch {}
                        }
                    });
                }
            }
            
            return Ok(new { message = $"Broadcast successfully sent to {users.Count} customers." });
        }

        public class BroadcastRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? ActionUrl { get; set; }
            public bool SendEmail { get; set; }
            public bool SendInApp { get; set; }
        }
`;

// Insert the endpoint right before the Category Management Endpoints section, or at the end
if (!adminApi.includes("SendBroadcast")) {
    adminApi = adminApi.replace('// Category Management Endpoints', broadcastEndpoint + '\n\n        // Category Management Endpoints');
    fs.writeFileSync('back/Controllers/AdminApiController.cs', adminApi, 'utf8');
}


// 2. FRONTEND HTML UPDATE (admin.html)
let adminHtml = fs.readFileSync('front/admin.html', 'utf8');
const broadcastHtml = `
                <!-- Broadcast Block -->
                <div class="card admin-form" style="padding: 1.5rem; margin-top: 1.5rem;">
                    <h3 style="color: var(--primary-gold); font-size: 1.1rem; margin-top: 0;">App-wide Broadcast (New Products / Sales)</h3>
                    <p style="font-size: 0.85rem; color: #666; margin-bottom: 1rem;">This sends a notification to ALL registered customers. Use sparingly for major product drops or sales.</p>
                    <div id="broadcastMsg" style="margin-bottom: 1rem; font-size: 0.9rem; display: none; padding: 10px; border-radius: 6px;"></div>
                    <form onsubmit="sendAppBroadcast(event)" style="display: flex; flex-direction: column; gap: 1rem;">
                        <input type="text" id="broadcastTitle" class="form-control" placeholder="Title (e.g., Massive Weekend Sale!)" required>
                        <textarea id="broadcastMessage" class="form-control" rows="4" placeholder="Message details..." required></textarea>
                        <input type="text" id="broadcastUrl" class="form-control" placeholder="Link URL (e.g., products.html)">
                        
                        <div style="display: flex; gap: 1rem; margin-bottom: 0.5rem; color: var(--primary-bg); font-weight: bold;">
                            <label style="cursor: pointer;"><input type="checkbox" id="broadcastInApp" checked> In-App Notification</label>
                            <label style="cursor: pointer;"><input type="checkbox" id="broadcastEmail"> Email Blast (Slower)</label>
                        </div>
                        
                        <button type="submit" id="broadcastBtn" class="btn btn-primary">Broadcast to All Customers</button>
                    </form>
                </div>
`;
if (!adminHtml.includes("sendAppBroadcast")) {
    adminHtml = adminHtml.replace('</form>\r\n                  </div>\r\n              </div>\r\n          </div>', '</form>\r\n                  </div>\r\n' + broadcastHtml + '              </div>\r\n          </div>');
    // Also try Unix line endings if that failed
    adminHtml = adminHtml.replace('</form>\n                  </div>\n              </div>\n          </div>', '</form>\n                  </div>\n' + broadcastHtml + '              </div>\n          </div>');
    fs.writeFileSync('front/admin.html', adminHtml, 'utf8');
}

// 3. FRONTEND JS UPDATE (admin.js)
let adminJs = fs.readFileSync('front/js/admin.js', 'utf8');
const broadcastJs = `
async function sendAppBroadcast(e) {
    e.preventDefault();
    const btn = document.getElementById('broadcastBtn');
    const msg = document.getElementById('broadcastMsg');
    
    if (!document.getElementById('broadcastInApp').checked && !document.getElementById('broadcastEmail').checked) {
        msg.textContent = 'Please select at least one method (In-App or Email).';
        msg.style.display = 'block';
        msg.style.backgroundColor = '#ffe5e5';
        msg.style.color = '#d63031';
        return;
    }
    
    btn.disabled = true;
    btn.textContent = 'Broadcasting...';
    msg.style.display = 'none';
    
    try {
        const payload = {
            title: document.getElementById('broadcastTitle').value,
            message: document.getElementById('broadcastMessage').value,
            actionUrl: document.getElementById('broadcastUrl').value,
            sendEmail: document.getElementById('broadcastEmail').checked,
            sendInApp: document.getElementById('broadcastInApp').checked
        };
        
        const res = await fetch('/api/admin/broadcast', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + localStorage.getItem('token') },
            body: JSON.stringify(payload)
        });
        
        const data = await res.json();
        if (res.ok) {
            msg.textContent = data.message;
            msg.style.backgroundColor = '#e5ffe5';
            msg.style.color = '#27ae60';
            document.getElementById('broadcastTitle').value = '';
            document.getElementById('broadcastMessage').value = '';
            document.getElementById('broadcastUrl').value = '';
        } else {
            msg.textContent = data.message || 'Failed to send broadcast.';
            msg.style.backgroundColor = '#ffe5e5';
            msg.style.color = '#d63031';
        }
    } catch (err) {
        msg.textContent = 'Error sending broadcast.';
        msg.style.backgroundColor = '#ffe5e5';
        msg.style.color = '#d63031';
    } finally {
        msg.style.display = 'block';
        btn.disabled = false;
        btn.textContent = 'Broadcast to All Customers';
    }
}
`;
if (!adminJs.includes("sendAppBroadcast")) {
    adminJs += "\n" + broadcastJs;
    fs.writeFileSync('front/js/admin.js', adminJs, 'utf8');
}

console.log("Broadcast features injected successfully!");
