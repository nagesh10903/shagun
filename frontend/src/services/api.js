import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000';

const api = axios.create({
  baseURL: API_BASE_URL,
});

// Request Interceptor: Attach JWT Token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('shagun_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Handle auth failure
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      // Clear token and reload or redirect if session expires
      localStorage.removeItem('shagun_token');
      localStorage.removeItem('shagun_user');
      if (!window.location.pathname.includes('/login') && !window.location.pathname.includes('/invite')) {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
