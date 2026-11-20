// Constantes compartidas. Se carga ANTES que auth.js en todas las páginas.
const SESSION_KEY = "deportes_california_usuario";
const CARRITO_KEY = "deportes_california_carrito";
const STOCK_BAJO = 5; // Debe coincidir con InventarioData.UmbralStockBajo en el backend
// URL base de la API. El frontend y la API se publican desde el mismo proyecto .NET (mismo origen).
// Si la API estuviera en otro servidor, solo se cambia aquí, por ejemplo: "https://mi-servidor.azurewebsites.net/api".
const API_URL = "/api";
