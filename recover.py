import json
import os

target_files = ["checkout.html", "products.html", "admin.html", "admin-exchanges.html", "admin-notifications.html", "transactions.html", "about.html", "contact.html", "faq.html", "cart.html", "privacy-policy.html", "product-detail.html", "replacement-request.html", "return-refund-policy.html", "return-tracking.html", "shipping-policy.html", "signup.html", "terms-and-conditions.html", "order-tracking.html", "notifications.html", "login.html"]

log_files = [
    r"C:\Users\STS\.gemini\antigravity\brain\ca532cca-7990-4ef1-a0d2-a9b5e199e68d\.system_generated\logs\transcript_full.jsonl",
    r"C:\Users\STS\.gemini\antigravity\brain\965a45e5-244c-4fa4-bee3-609b5e25c5f2\.system_generated\logs\transcript_full.jsonl"
]

file_contents = {}

for log_path in reversed(log_files): # Start with older, then newer
    if not os.path.exists(log_path):
        continue
        
    with open(log_path, 'r', encoding='utf-8') as f:
        for line in f:
            try:
                data = json.loads(line)
                if data.get('type') == 'PLANNER_RESPONSE':
                    tool_calls = data.get('tool_calls', [])
                    for call in tool_calls:
                        if call.get('name') == 'write_to_file' or call.get('name') == 'default_api:write_to_file':
                            args = call.get('args', {})
                            target = args.get('TargetFile', '')
                            for tf in target_files:
                                if tf in target:
                                    if 'CodeContent' in args:
                                        file_contents[tf] = args['CodeContent']
                                        
                        # Note: if they used replace_file_content heavily, this might miss partial edits,
                        # but often write_to_file is used to create or rewrite.
                        # Also check view_file outputs for full content? 
                        # That's in RUN_COMMAND or generic responses.
            except Exception as e:
                pass

for fname, content in file_contents.items():
    out_path = os.path.join(r"c:\Users\STS\Desktop\LeatherLane Atelier\front", fname)
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(content)
        
print("Recovered files: " + ", ".join(file_contents.keys()))
