export function base64UrlEncode(str: string) {
  // 1. Standard Base64 encode
  const base64 = btoa(encodeURIComponent(str).replace(/%([0-9A-F]{2})/g, (_match, p1) => {
    return String.fromCharCode(parseInt(p1, 16));
  }));

  // 2. Convert to Base64URL
  return base64
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}

export function base64UrlDecode(base64url: string) {
  // 1. Convert back to standard Base64
  let base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
  while (base64.length % 4) {
    base64 += '=';
  }

  // 2. Standard Base64 decode
  return decodeURIComponent(
    atob(base64)
      .split('')
      .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
      .join('')
  );
}

export function base64UrlEncodeBytes(bytes: Uint8Array) {
    let binary = '';
    const chunk = 0x8000;
    for (let i = 0; i < bytes.length; i += chunk) {
        binary += String.fromCharCode(...bytes.subarray(i, i + chunk));
    }
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}