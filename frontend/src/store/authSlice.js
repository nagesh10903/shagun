import { createSlice } from '@reduxjs/toolkit';

const initialToken = localStorage.getItem('shagun_token') || null;
let initialUser = null;
try {
  const savedUser = localStorage.getItem('shagun_user');
  if (savedUser) {
    initialUser = JSON.parse(savedUser);
  }
} catch (e) {
  localStorage.removeItem('shagun_user');
}

const authSlice = createSlice({
  name: 'auth',
  initialState: {
    token: initialToken,
    user: initialUser,
    isAuthenticated: !!initialToken,
    loading: false,
    error: null,
  },
  reducers: {
    loginStart(state) {
      state.loading = true;
      state.error = null;
    },
    loginSuccess(state, action) {
      state.loading = false;
      state.token = action.payload.access_token;
      state.user = action.payload.user;
      state.isAuthenticated = true;
      localStorage.setItem('shagun_token', action.payload.access_token);
      localStorage.setItem('shagun_user', JSON.stringify(action.payload.user));
    },
    loginFailure(state, action) {
      state.loading = false;
      state.error = action.payload;
      state.token = null;
      state.user = null;
      state.isAuthenticated = false;
      localStorage.removeItem('shagun_token');
      localStorage.removeItem('shagun_user');
    },
    logout(state) {
      state.token = null;
      state.user = null;
      state.isAuthenticated = false;
      localStorage.removeItem('shagun_token');
      localStorage.removeItem('shagun_user');
    },
    updateUser(state, action) {
      state.user = action.payload;
      localStorage.setItem('shagun_user', JSON.stringify(action.payload));
    }
  },
});

export const { loginStart, loginSuccess, loginFailure, logout, updateUser } = authSlice.actions;
export default authSlice.reducer;
