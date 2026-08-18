
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
    const image = document.getElementById('storyCoverImage').value;
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
