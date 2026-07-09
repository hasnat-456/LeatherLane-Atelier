import React, { useState, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import { CartContext } from '../context/CartContext';
import OTPVerification from '../components/OTPVerification';
import api from '../api/axios';

const Checkout = () => {
  const [paymentMethod, setPaymentMethod] = useState('jazzcash');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [cardNumber, setCardNumber] = useState('');
  const [cardExpiry, setCardExpiry] = useState('');
  const [cardCvc, setCardCvc] = useState('');
  const [showPaymentOTP, setShowPaymentOTP] = useState(false);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const { user } = useContext(AuthContext);
  const { cart, getCartTotal, clearCart } = useContext(CartContext);
  const navigate = useNavigate();

  const total = getCartTotal();

  const handleInitiatePayment = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    let paymentDetails = {};
    if (paymentMethod === 'jazzcash' || paymentMethod === 'easypaisa') {
      paymentDetails = { phoneNumber };
    } else if (paymentMethod === 'card') {
      paymentDetails = { cardLast4: cardNumber.slice(-4) };
    }

    try {
      await api.post('/transactions/initiate-payment', {
        items: cart,
        totalAmount: total,
        paymentMethod,
        paymentDetails,
      });
      setShowPaymentOTP(true);
    } catch (err) {
      setError(err.response?.data?.message || 'Payment initiation failed');
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyPayment = async (otp) => {
    const { data } = await api.post('/transactions/verify-payment', { otp });
    clearCart();
    setSuccess(true);
    setTimeout(() => navigate('/transactions'), 2000);
  };

  const handleResendPaymentOTP = async () => {
    // Re-initiate payment to resend OTP
    let paymentDetails = {};
    if (paymentMethod === 'jazzcash' || paymentMethod === 'easypaisa') {
      paymentDetails = { phoneNumber };
    } else if (paymentMethod === 'card') {
      paymentDetails = { cardLast4: cardNumber.slice(-4) };
    }
    await api.post('/transactions/initiate-payment', {
      items: cart,
      totalAmount: total,
      paymentMethod,
      paymentDetails,
    });
  };

  if (!user) {
    return (
      <div style={styles.container}>
        <p style={styles.message}>Please login to checkout</p>
      </div>
    );
  }

  if (cart.length === 0) {
    return (
      <div style={styles.container}>
        <p style={styles.message}>Your cart is empty</p>
      </div>
    );
  }

  if (success) {
    return (
      <div style={styles.container}>
        <div style={styles.successCard}>
          <h2>Payment Successful!</h2>
          <p>Redirecting to transactions...</p>
        </div>
      </div>
    );
  }

  if (showPaymentOTP) {
    return (
      <div style={styles.container}>
        <OTPVerification
          onVerify={handleVerifyPayment}
          onResend={handleResendPaymentOTP}
          email={user.email}
          message="Please verify your payment."
        />
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <h2 style={styles.title}>Checkout</h2>
      <div style={styles.checkoutWrapper}>
        <div style={styles.orderSummary}>
          <h3>Order Summary</h3>
          {cart.map((item, idx) => (
            <div key={idx} style={styles.item}>
              <span>{item.name} x {item.quantity}</span>
              <span>${item.price * item.quantity}</span>
            </div>
          ))}
          <div style={styles.total}>
            <span>Total:</span>
            <span>${total}</span>
          </div>
        </div>

        <form onSubmit={handleInitiatePayment} style={styles.form}>
          {error && <p style={styles.error}>{error}</p>}
          <div style={styles.formGroup}>
            <label style={styles.label}>Payment Method</label>
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              style={styles.select}
            >
              <option value="jazzcash">JazzCash</option>
              <option value="easypaisa">EasyPaisa</option>
              <option value="card">Credit/Debit Card</option>
            </select>
          </div>

          {(paymentMethod === 'jazzcash' || paymentMethod === 'easypaisa') && (
            <div style={styles.formGroup}>
              <label style={styles.label}>
                {paymentMethod === 'jazzcash' ? 'JazzCash' : 'EasyPaisa'} Phone Number
              </label>
              <input
                type="tel"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                placeholder="03XXXXXXXXX"
                required
                style={styles.input}
              />
            </div>
          )}

          {paymentMethod === 'card' && (
            <>
              <div style={styles.formGroup}>
                <label style={styles.label}>Card Number</label>
                <input
                  type="text"
                  value={cardNumber}
                  onChange={(e) => setCardNumber(e.target.value)}
                  placeholder="XXXX XXXX XXXX XXXX"
                  required
                  maxLength={19}
                  style={styles.input}
                />
              </div>
              <div style={styles.formRow}>
                <div style={styles.formGroup}>
                  <label style={styles.label}>Expiry Date</label>
                  <input
                    type="text"
                    value={cardExpiry}
                    onChange={(e) => setCardExpiry(e.target.value)}
                    placeholder="MM/YY"
                    required
                    maxLength={5}
                    style={styles.input}
                  />
                </div>
                <div style={styles.formGroup}>
                  <label style={styles.label}>CVC</label>
                  <input
                    type="text"
                    value={cardCvc}
                    onChange={(e) => setCardCvc(e.target.value)}
                    placeholder="XXX"
                    required
                    maxLength={3}
                    style={styles.input}
                  />
                </div>
              </div>
            </>
          )}

          <button
            type="submit"
            disabled={loading}
            style={{ ...styles.button, opacity: loading ? 0.7 : 1 }}
          >
            {loading ? 'Processing...' : 'Proceed to Payment'}
          </button>
        </form>
      </div>
    </div>
  );
};

const styles = {
  container: {
    maxWidth: '1000px',
    margin: '0 auto',
    padding: '2rem',
  },
  title: {
    fontSize: '2rem',
    marginBottom: '2rem',
    color: '#3d2914',
    textAlign: 'center',
  },
  checkoutWrapper: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '2rem',
  },
  orderSummary: {
    backgroundColor: '#fff',
    padding: '2rem',
    borderRadius: '8px',
    boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
  },
  item: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: '0.8rem 0',
    borderBottom: '1px solid #eee',
  },
  total: {
    display: 'flex',
    justifyContent: 'space-between',
    paddingTop: '1rem',
    fontSize: '1.3rem',
    fontWeight: 'bold',
    color: '#3d2914',
  },
  form: {
    backgroundColor: '#fff',
    padding: '2rem',
    borderRadius: '8px',
    boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
    display: 'flex',
    flexDirection: 'column',
    gap: '1.5rem',
  },
  error: {
    color: '#c0392b',
    textAlign: 'center',
  },
  formRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '1rem',
  },
  formGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem',
  },
  label: {
    color: '#5c3d2e',
    fontWeight: 'bold',
  },
  select: {
    padding: '0.8rem',
    borderRadius: '4px',
    border: '1px solid #ddd',
    fontSize: '1rem',
  },
  input: {
    padding: '0.8rem',
    borderRadius: '4px',
    border: '1px solid #ddd',
    fontSize: '1rem',
  },
  button: {
    backgroundColor: '#5c3d2e',
    color: '#fff',
    padding: '1rem',
    borderRadius: '4px',
    border: 'none',
    cursor: 'pointer',
    fontSize: '1.1rem',
    transition: 'background-color 0.3s',
  },
  message: {
    textAlign: 'center',
    fontSize: '1.2rem',
    color: '#666',
    marginTop: '3rem',
  },
  successCard: {
    backgroundColor: '#d5f5e3',
    color: '#27ae60',
    padding: '3rem',
    borderRadius: '8px',
    textAlign: 'center',
  },
};

export default Checkout;
