import React, { useContext } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import { CartContext } from '../context/CartContext';

const Header = () => {
  const { user, logout } = useContext(AuthContext);
  const { getCartCount } = useContext(CartContext);
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <header style={styles.header}>
      <div style={styles.container}>
        <Link to="/" style={styles.logo}>
          <span style={styles.logoText}>LeatherLane</span>
          <span style={styles.logoSub}>Atelier</span>
        </Link>
        <nav style={styles.nav}>
          <Link to="/" style={styles.link}>Home</Link>
          <Link to="/about" style={styles.link}>About</Link>
          <Link to="/contact" style={styles.link}>Contact</Link>
          {user && <Link to="/transactions" style={styles.link}>Orders</Link>}
          <Link to="/cart" style={styles.cartLink}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="9" cy="21" r="1" />
              <circle cx="20" cy="21" r="1" />
              <path d="M1 1h4l2.68 13.39a2 2 0 002 1.61h9.72a2 2 0 002-1.61L23 6H6" />
            </svg>
            {getCartCount() > 0 && <span style={styles.cartBadge}>{getCartCount()}</span>}
          </Link>
          {user ? (
            <div style={styles.userSection}>
              <span style={styles.userName}>{user.name}</span>
              <button onClick={handleLogout} style={styles.button}>Logout</button>
            </div>
          ) : (
            <div style={styles.authSection}>
              <Link to="/login" style={styles.link}>Login</Link>
              <Link to="/register" style={styles.button}>Register</Link>
            </div>
          )}
        </nav>
      </div>
    </header>
  );
};

const styles = {
  header: {
    backgroundColor: '#5c3d2e',
    color: '#f5f0eb',
    padding: '1.25rem 0',
    boxShadow: '0 4px 20px rgba(0,0,0,0.15)',
    position: 'sticky',
    top: 0,
    zIndex: 1000
  },
  container: {
    maxWidth: '1280px',
    margin: '0 auto',
    padding: '0 2rem',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  logo: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-start'
  },
  logoText: {
    fontSize: '1.75rem',
    fontWeight: '700',
    letterSpacing: '3px'
  },
  logoSub: {
    fontSize: '0.75rem',
    letterSpacing: '2px',
    opacity: '0.9'
  },
  nav: {
    display: 'flex',
    gap: '2rem',
    alignItems: 'center'
  },
  link: {
    color: '#f5f0eb',
    fontSize: '1rem',
    fontWeight: '500',
    padding: '0.5rem 0',
    borderBottom: '2px solid transparent',
    transition: 'all 0.3s ease',
    '&:hover': {
      color: '#d4af37',
      borderBottom: '2px solid #d4af37'
    }
  },
  cartLink: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    color: '#f5f0eb',
    transition: 'all 0.3s ease',
    '&:hover': {
      color: '#d4af37'
    }
  },
  cartBadge: {
    position: 'absolute',
    top: '-8px',
    right: '-12px',
    backgroundColor: '#d4af37',
    color: '#3d2914',
    borderRadius: '50%',
    width: '20px',
    height: '20px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '0.75rem',
    fontWeight: '700',
    boxShadow: '0 2px 8px rgba(212,175,55,0.3)'
  },
  userSection: {
    display: 'flex',
    gap: '1rem',
    alignItems: 'center'
  },
  userName: {
    fontSize: '1rem'
  },
  authSection: {
    display: 'flex',
    gap: '1rem',
    alignItems: 'center'
  },
  button: {
    backgroundColor: '#d4af37',
    color: '#3d2914',
    padding: '0.6rem 1.5rem',
    borderRadius: '6px',
    border: 'none',
    fontSize: '0.95rem',
    fontWeight: '600',
    transition: 'all 0.3s ease',
    boxShadow: '0 2px 8px rgba(212,175,55,0.2)',
    '&:hover': {
      backgroundColor: '#b8962e',
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 12px rgba(212,175,55,0.3)'
    }
  }
};

export default Header;
