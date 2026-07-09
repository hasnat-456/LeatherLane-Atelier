import React, { useState } from 'react';

const OTPVerification = ({ onVerify, onResend, email, message }) => {
  const [otp, setOtp] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!otp || otp.length !== 6) {
      setError('Please enter a valid 6-digit OTP');
      return;
    }
    setError('');
    setLoading(true);
    try {
      await onVerify(otp);
    } catch (err) {
      setError(err.response?.data?.message || 'Verification failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.container}>
      <h2 style={styles.title}>Verify OTP</h2>
      {message && <p style={styles.message}>{message}</p>}
      <p style={styles.info}>
        OTP sent to {email}
      </p>
      {error && <p style={styles.error}>{error}</p>}
      <form onSubmit={handleSubmit} style={styles.form}>
        <div style={styles.inputGroup}>
          <input
            type="text"
            value={otp}
            onChange={(e) => setOtp(e.target.value.replace(/\D/g, ''))}
            placeholder="Enter 6-digit OTP"
            maxLength={6}
            style={styles.input}
          />
        </div>
        <button
          type="submit"
          disabled={loading}
          style={{ ...styles.button, opacity: loading ? 0.7 : 1 }}
        >
          {loading ? 'Verifying...' : 'Verify OTP'}
        </button>
      </form>
      <button onClick={onResend} style={styles.resendButton}>
        Resend OTP
      </button>
    </div>
  );
};

const styles = {
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    padding: '2rem',
    backgroundColor: '#fff',
    borderRadius: '8px',
    boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
  },
  title: {
    color: '#3d2914',
    marginBottom: '1rem',
  },
  info: {
    color: '#5c3d2e',
    marginBottom: '1.5rem',
  },
  message: {
    color: '#27ae60',
    marginBottom: '1rem',
    textAlign: 'center',
  },
  error: {
    color: '#c0392b',
    marginBottom: '1rem',
  },
  form: {
    width: '100%',
    maxWidth: '300px',
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem',
  },
  inputGroup: {
    display: 'flex',
    flexDirection: 'column',
  },
  input: {
    padding: '1rem',
    fontSize: '1.2rem',
    textAlign: 'center',
    letterSpacing: '0.5rem',
    border: '1px solid #ddd',
    borderRadius: '4px',
  },
  button: {
    backgroundColor: '#5c3d2e',
    color: '#fff',
    padding: '1rem',
    borderRadius: '4px',
    border: 'none',
    cursor: 'pointer',
    fontSize: '1rem',
  },
  resendButton: {
    marginTop: '1rem',
    backgroundColor: 'transparent',
    color: '#8b5a2b',
    border: 'none',
    cursor: 'pointer',
    fontSize: '0.9rem',
    textDecoration: 'underline',
  },
};

export default OTPVerification;
