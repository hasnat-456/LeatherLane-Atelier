const express = require('express');
const jwt = require('jsonwebtoken');
const User = require('../models/User');
const sendEmail = require('../utils/email');
const { protect } = require('../middleware/auth');

const router = express.Router();

const generateToken = (id) => {
  return jwt.sign({ id }, process.env.JWT_SECRET, { expiresIn: '30d' });
};

const generateOTP = () => {
  return Math.floor(100000 + Math.random() * 900000).toString();
};

router.post('/register', async (req, res) => {
  const { name, email, password, phone } = req.body;

  try {
    const userExists = await User.findOne({ email });

    if (userExists) {
      return res.status(400).json({ message: 'User already exists' });
    }

    const otp = generateOTP();
    const otpExpires = Date.now() + 10 * 60 * 1000; // 10 minutes

    const user = await User.create({
      name,
      email,
      phone,
      password,
      verificationOTP: otp,
      verificationOTPExpires: otpExpires,
    });

    await sendEmail({
      email: user.email,
      subject: 'Verify your email - LeatherLane Atelier',
      text: `Your OTP for email verification is: ${otp}\n\nThis OTP will expire in 10 minutes.`,
      html: `<p>Your OTP for email verification is: <strong>${otp}</strong></p><p>This OTP will expire in 10 minutes.</p>`,
    });

    console.log(`OTP for ${email}: ${otp}`);

    res.status(201).json({
      _id: user._id,
      name: user.name,
      email: user.email,
      phone: user.phone,
      isVerified: user.isVerified,
      message: 'OTP sent to email',
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

router.post('/verify-otp', async (req, res) => {
  const { email, otp } = req.body;

  try {
    const user = await User.findOne({
      email,
      verificationOTP: otp,
      verificationOTPExpires: { $gt: Date.now() },
    });

    if (!user) {
      return res.status(400).json({ message: 'Invalid or expired OTP' });
    }

    user.isVerified = true;
    user.verificationOTP = undefined;
    user.verificationOTPExpires = undefined;
    await user.save();

    res.json({
      _id: user._id,
      name: user.name,
      email: user.email,
      isVerified: user.isVerified,
      token: generateToken(user._id),
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

router.post('/resend-otp', async (req, res) => {
  const { email } = req.body;

  try {
    const user = await User.findOne({ email });

    if (!user) {
      return res.status(404).json({ message: 'User not found' });
    }

    if (user.isVerified) {
      return res.status(400).json({ message: 'User already verified' });
    }

    const otp = generateOTP();
    const otpExpires = Date.now() + 10 * 60 * 1000;

    user.verificationOTP = otp;
    user.verificationOTPExpires = otpExpires;
    await user.save();

    await sendEmail({
      email: user.email,
      subject: 'Resend OTP - LeatherLane Atelier',
      text: `Your new OTP is: ${otp}\n\nThis OTP will expire in 10 minutes.`,
      html: `<p>Your new OTP is: <strong>${otp}</strong></p><p>This OTP will expire in 10 minutes.</p>`,
    });

    console.log(`Resent OTP for ${email}: ${otp}`);

    res.json({ message: 'OTP resent' });
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

router.post('/login', async (req, res) => {
  const { email, password } = req.body;

  try {
    const user = await User.findOne({ email });

    if (!user || !(await user.matchPassword(password))) {
      return res.status(401).json({ message: 'Invalid email or password' });
    }

    if (!user.isVerified) {
      const otp = generateOTP();
      const otpExpires = Date.now() + 10 * 60 * 1000;

      user.verificationOTP = otp;
      user.verificationOTPExpires = otpExpires;
      await user.save();

      await sendEmail({
        email: user.email,
        subject: 'Verify your email - LeatherLane Atelier',
        text: `Your OTP for email verification is: ${otp}\n\nThis OTP will expire in 10 minutes.`,
        html: `<p>Your OTP for email verification is: <strong>${otp}</strong></p><p>This OTP will expire in 10 minutes.</p>`,
      });

      console.log(`OTP for ${email}: ${otp}`);

      return res.json({
        _id: user._id,
        name: user.name,
        email: user.email,
        isVerified: user.isVerified,
        message: 'OTP sent to email',
      });
    }

    res.json({
      _id: user._id,
      name: user.name,
      email: user.email,
      token: generateToken(user._id),
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ message: 'Server error' });
  }
});

router.get('/me', protect, async (req, res) => {
  res.json({
    _id: req.user._id,
    name: req.user.name,
    email: req.user.email,
    phone: req.user.phone,
    isVerified: req.user.isVerified,
  });
});

module.exports = router;
