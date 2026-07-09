const express = require('express');
const Transaction = require('../models/Transaction');
const User = require('../models/User');
const sendEmail = require('../utils/email');
const { protect } = require('../middleware/auth');

const router = express.Router();

const generateOTP = () => {
  return Math.floor(100000 + Math.random() * 900000).toString();
};

router.post('/initiate-payment', protect, async (req, res) => {
  const { items, totalAmount, paymentMethod, paymentDetails } = req.body;

  try {
    const user = await User.findById(req.user._id);
    const otp = generateOTP();

    let tempTransaction = {
      user: req.user._id,
      items,
      totalAmount,
      paymentMethod,
      paymentDetails,
      tempOTP: otp,
      tempOTPExpires: Date.now() + 10 * 60 * 1000,
    };

    req.session = req.session || {};
    req.session.tempTransaction = tempTransaction;

    const contact = user.email || paymentDetails?.phoneNumber;
    if (contact) {
      if (user.email) {
        await sendEmail({
          email: user.email,
          subject: 'Payment Verification - LeatherLane Atelier',
          text: `Your OTP for payment verification is: ${otp}\n\nThis OTP will expire in 10 minutes.`,
          html: `<p>Your OTP for payment verification is: <strong>${otp}</strong></p><p>This OTP will expire in 10 minutes.</p>`,
        });
      }
      console.log(`Payment OTP for ${user.email || contact}: ${otp}`);
    }

    res.json({ message: 'OTP sent for payment verification' });
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

router.post('/verify-payment', protect, async (req, res) => {
  const { otp } = req.body;

  try {
    const tempTransaction = req.session?.tempTransaction;
    if (!tempTransaction) {
      return res.status(400).json({ message: 'No pending payment' });
    }

    const isExpired = Date.now() > tempTransaction.tempOTPExpires;
    const isOTPValid = tempTransaction.tempOTP === otp;

    if (isExpired || !isOTPValid) {
      return res.status(400).json({ message: 'Invalid or expired OTP' });
    }

    const transaction = new Transaction({
      user: tempTransaction.user,
      items: tempTransaction.items,
      totalAmount: tempTransaction.totalAmount,
      paymentMethod: tempTransaction.paymentMethod,
      paymentDetails: tempTransaction.paymentDetails,
      status: 'completed',
    });

    await transaction.save();

    req.session.tempTransaction = undefined;

    res.status(201).json(transaction);
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

router.get('/', protect, async (req, res) => {
  try {
    const transactions = await Transaction.find({ user: req.user._id }).sort({ createdAt: -1 });
    res.json(transactions);
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

router.get('/:id', protect, async (req, res) => {
  try {
    const transaction = await Transaction.findById(req.params.id);

    if (transaction && transaction.user.toString() === req.user._id.toString()) {
      res.json(transaction);
    } else {
      res.status(404).json({ message: 'Transaction not found' });
    }
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

module.exports = router;
