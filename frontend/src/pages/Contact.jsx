import React, { useState } from 'react';

const Contact = () => {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    subject: '',
    message: ''
  });
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = (e) => {
    e.preventDefault();
    console.log('Form submitted:', formData);
    setSubmitted(true);
    setTimeout(() => {
      setFormData({ name: '', email: '', subject: '', message: '' });
      setSubmitted(false);
    }, 3000);
  };

  const handleChange = (e) => {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
  };

  return (
    <div style={styles.container}>
      {/* Hero Section */}
      <section style={styles.hero}>
        <div style={styles.heroContent}>
          <h1 style={styles.heroTitle}>Get in Touch</h1>
          <p style={styles.heroSubtitle}>We'd love to hear from you</p>
        </div>
      </section>

      <section style={styles.contactSection}>
        <div style={styles.contactGrid}>
          {/* Contact Info */}
          <div style={styles.contactInfo}>
            <h2 style={styles.infoTitle}>Contact Information</h2>
            <div style={styles.infoCard}>
              <div style={styles.infoItem}>
                <span style={styles.infoIcon}>📍</span>
                <div>
                  <h4 style={styles.infoItemTitle}>Address</h4>
                  <p style={styles.infoItemText}>123 Leather Lane, Artisan District<br />New York, NY 10001</p>
                </div>
              </div>
              <div style={styles.infoItem}>
                <span style={styles.infoIcon}>📧</span>
                <div>
                  <h4 style={styles.infoItemTitle}>Email</h4>
                  <p style={styles.infoItemText}>hello@leatherlane.com<br />support@leatherlane.com</p>
                </div>
              </div>
              <div style={styles.infoItem}>
                <span style={styles.infoIcon}>📞</span>
                <div>
                  <h4 style={styles.infoItemTitle}>Phone</h4>
                  <p style={styles.infoItemText}>+1 (555) 123-4567<br />Mon-Fri, 9AM-6PM EST</p>
                </div>
              </div>
            </div>
          </div>

          {/* Contact Form */}
          <div style={styles.formContainer}>
            {submitted ? (
              <div style={styles.successMessage}>
                <h3 style={styles.successTitle}>Thank You!</h3>
                <p style={styles.successText}>Your message has been sent successfully. We'll get back to you soon.</p>
              </div>
            ) : (
              <form onSubmit={handleSubmit} style={styles.form}>
                <h2 style={styles.formTitle}>Send us a Message</h2>
                <div style={styles.formGroup}>
                  <label style={styles.label}>Your Name</label>
                  <input
                    type="text"
                    name="name"
                    value={formData.name}
                    onChange={handleChange}
                    required
                    style={styles.input}
                  />
                </div>
                <div style={styles.formGroup}>
                  <label style={styles.label}>Email Address</label>
                  <input
                    type="email"
                    name="email"
                    value={formData.email}
                    onChange={handleChange}
                    required
                    style={styles.input}
                  />
                </div>
                <div style={styles.formGroup}>
                  <label style={styles.label}>Subject</label>
                  <input
                    type="text"
                    name="subject"
                    value={formData.subject}
                    onChange={handleChange}
                    required
                    style={styles.input}
                  />
                </div>
                <div style={styles.formGroup}>
                  <label style={styles.label}>Message</label>
                  <textarea
                    name="message"
                    value={formData.message}
                    onChange={handleChange}
                    required
                    rows="6"
                    style={styles.textarea}
                  />
                </div>
                <button type="submit" style={styles.button}>
                  Send Message
                </button>
              </form>
            )}
          </div>
        </div>
      </section>
    </div>
  );
};

const styles = {
  container: {
    width: '100%'
  },
  hero: {
    background: 'linear-gradient(135deg, #5c3d2e 0%, #8b5a2b 100%)',
    padding: '4rem 2rem',
    textAlign: 'center',
    color: '#f5f0eb'
  },
  heroContent: {
    maxWidth: '800px',
    margin: '0 auto'
  },
  heroTitle: {
    fontSize: '2.8rem',
    marginBottom: '0.75rem',
    fontWeight: '700'
  },
  heroSubtitle: {
    fontSize: '1.2rem',
    opacity: '0.9'
  },
  contactSection: {
    padding: '5rem 2rem',
    backgroundColor: '#ffffff'
  },
  contactGrid: {
    maxWidth: '1100px',
    margin: '0 auto',
    display: 'grid',
    gridTemplateColumns: '1fr 1.5fr',
    gap: '3rem'
  },
  contactInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2rem'
  },
  infoTitle: {
    fontSize: '2rem',
    color: '#3d2914',
    marginBottom: '1rem'
  },
  infoCard: {
    backgroundColor: '#f5f0eb',
    padding: '2.5rem',
    borderRadius: '16px',
    boxShadow: '0 4px 16px rgba(92,61,46,0.08)'
  },
  infoItem: {
    display: 'flex',
    gap: '1.25rem',
    marginBottom: '2rem',
    '&:last-child': {
      marginBottom: 0
    }
  },
  infoIcon: {
    fontSize: '2rem',
    flexShrink: 0
  },
  infoItemTitle: {
    fontSize: '1.15rem',
    color: '#3d2914',
    marginBottom: '0.25rem'
  },
  infoItemText: {
    color: '#5c4a3a',
    lineHeight: '1.7'
  },
  formContainer: {
    padding: '2.5rem',
    backgroundColor: '#f5f0eb',
    borderRadius: '16px',
    boxShadow: '0 4px 16px rgba(92,61,46,0.08)'
  },
  form: {
    width: '100%'
  },
  formTitle: {
    fontSize: '1.8rem',
    color: '#3d2914',
    marginBottom: '2rem'
  },
  formGroup: {
    marginBottom: '1.5rem'
  },
  label: {
    display: 'block',
    color: '#5c3d2e',
    fontWeight: '600',
    marginBottom: '0.5rem',
    fontSize: '1rem'
  },
  input: {
    width: '100%',
    padding: '0.9rem 1.25rem',
    borderRadius: '8px',
    border: '2px solid #e0d9d3',
    fontSize: '1rem',
    backgroundColor: '#ffffff',
    transition: 'all 0.3s ease',
    '&:focus': {
      outline: 'none',
      borderColor: '#d4af37',
      boxShadow: '0 0 0 3px rgba(212,175,55,0.1)'
    }
  },
  textarea: {
    width: '100%',
    padding: '0.9rem 1.25rem',
    borderRadius: '8px',
    border: '2px solid #e0d9d3',
    fontSize: '1rem',
    fontFamily: 'inherit',
    backgroundColor: '#ffffff',
    resize: 'vertical',
    transition: 'all 0.3s ease',
    '&:focus': {
      outline: 'none',
      borderColor: '#d4af37',
      boxShadow: '0 0 0 3px rgba(212,175,55,0.1)'
    }
  },
  button: {
    width: '100%',
    backgroundColor: '#5c3d2e',
    color: '#f5f0eb',
    padding: '1rem 2rem',
    borderRadius: '8px',
    border: 'none',
    fontSize: '1.1rem',
    fontWeight: '600',
    cursor: 'pointer',
    transition: 'all 0.3s ease',
    marginTop: '0.5rem',
    '&:hover': {
      backgroundColor: '#8b5a2b',
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 12px rgba(92,61,46,0.2)'
    }
  },
  successMessage: {
    textAlign: 'center',
    padding: '3rem 2rem',
    backgroundColor: 'rgba(39,174,96,0.1)',
    borderRadius: '12px'
  },
  successTitle: {
    fontSize: '1.8rem',
    color: '#27ae60',
    marginBottom: '0.75rem'
  },
  successText: {
    color: '#5c4a3a',
    fontSize: '1.05rem'
  }
};

export default Contact;
