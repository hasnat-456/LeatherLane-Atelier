import React from 'react';
import { Link } from 'react-router-dom';

const Footer = () => {
  return (
    <footer style={styles.footer}>
      <div style={styles.container}>
        <div style={styles.grid}>
          {/* Brand */}
          <div style={styles.section}>
            <h3 style={styles.brandName}>LeatherLane Atelier</h3>
            <p style={styles.brandDesc}>
              Crafting timeless leather goods with passion, precision, and the finest materials since 1985.
            </p>
            <div style={styles.socialLinks}>
              <a href="#" style={styles.socialLink}>📘</a>
              <a href="#" style={styles.socialLink}>📷</a>
              <a href="#" style={styles.socialLink}>🐦</a>
            </div>
          </div>

          {/* Quick Links */}
          <div style={styles.section}>
            <h4 style={styles.sectionTitle}>Quick Links</h4>
            <ul style={styles.linkList}>
              <li><Link to="/" style={styles.link}>Home</Link></li>
              <li><Link to="/about" style={styles.link}>About Us</Link></li>
              <li><Link to="/contact" style={styles.link}>Contact</Link></li>
              <li><Link to="/cart" style={styles.link}>Cart</Link></li>
            </ul>
          </div>

          {/* Customer Service */}
          <div style={styles.section}>
            <h4 style={styles.sectionTitle}>Customer Service</h4>
            <ul style={styles.linkList}>
              <li><a href="#" style={styles.link}>Shipping & Returns</a></li>
              <li><a href="#" style={styles.link}>FAQ</a></li>
              <li><a href="#" style={styles.link}>Warranty</a></li>
              <li><a href="#" style={styles.link}>Size Guide</a></li>
            </ul>
          </div>

          {/* Contact Info */}
          <div style={styles.section}>
            <h4 style={styles.sectionTitle}>Contact Us</h4>
            <ul style={styles.linkList}>
              <li style={styles.contactItem}>📍 123 Leather Lane, NY 10001</li>
              <li style={styles.contactItem}>📧 hello@leatherlane.com</li>
              <li style={styles.contactItem}>📞 +1 (555) 123-4567</li>
            </ul>
          </div>
        </div>

        <div style={styles.bottom}>
          <p style={styles.copyright}>&copy; 2025 LeatherLane Atelier. All rights reserved.</p>
          <div style={styles.bottomLinks}>
            <a href="#" style={styles.bottomLink}>Privacy Policy</a>
            <a href="#" style={styles.bottomLink}>Terms of Service</a>
          </div>
        </div>
      </div>
    </footer>
  );
};

const styles = {
  footer: {
    backgroundColor: '#3d2914',
    color: '#f5f0eb',
    padding: '4rem 2rem 2rem'
  },
  container: {
    maxWidth: '1280px',
    margin: '0 auto'
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
    gap: '3rem',
    marginBottom: '3rem',
    paddingBottom: '2.5rem',
    borderBottom: '1px solid rgba(245,240,235,0.2)'
  },
  section: {},
  brandName: {
    fontSize: '1.6rem',
    fontWeight: '700',
    marginBottom: '1rem',
    letterSpacing: '1px'
  },
  brandDesc: {
    color: 'rgba(245,240,235,0.8)',
    lineHeight: '1.7',
    marginBottom: '1.5rem',
    fontSize: '0.95rem'
  },
  socialLinks: {
    display: 'flex',
    gap: '1rem'
  },
  socialLink: {
    fontSize: '1.5rem',
    color: '#f5f0eb',
    transition: 'all 0.3s ease',
    '&:hover': {
      color: '#d4af37',
      transform: 'translateY(-3px)'
    }
  },
  sectionTitle: {
    fontSize: '1.15rem',
    fontWeight: '600',
    marginBottom: '1.25rem',
    color: '#d4af37'
  },
  linkList: {
    listStyle: 'none',
    padding: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: '0.75rem'
  },
  link: {
    color: 'rgba(245,240,235,0.85)',
    transition: 'all 0.3s ease',
    fontSize: '0.95rem',
    '&:hover': {
      color: '#d4af37',
      paddingLeft: '4px'
    }
  },
  contactItem: {
    color: 'rgba(245,240,235,0.85)',
    fontSize: '0.95rem'
  },
  bottom: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: '1rem'
  },
  copyright: {
    color: 'rgba(245,240,235,0.7)',
    fontSize: '0.9rem'
  },
  bottomLinks: {
    display: 'flex',
    gap: '1.5rem'
  },
  bottomLink: {
    color: 'rgba(245,240,235,0.7)',
    fontSize: '0.9rem',
    transition: 'all 0.3s ease',
    '&:hover': {
      color: '#d4af37'
    }
  }
};

export default Footer;
