import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiClient {
  // Android emulator'de 10.0.2.2, host makinenin localhost'udur (backend: http://localhost:5223).
  // Açık metin HTTP sadece bu adres için izinli (android/app/src/main/res/xml/network_security_config.xml).
  // Farklı sunucu için: flutter run --dart-define=API_URL=https://sunucu/api
  static const String baseUrl = String.fromEnvironment(
    'API_URL',
    defaultValue: 'http://10.0.2.2:5223/api',
  );

  static const _timeout = Duration(seconds: 20);

  /// Refresh token da geçersizse çağrılır (main.dart login ekranına yönlendirir).
  static void Function()? onSessionExpired;

  static Future<bool>? _refreshing;

  static Future<String?> _getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('accessToken');
  }

  static Future<Map<String, String>> _headers({bool auth = true}) async {
    final headers = {'Content-Type': 'application/json'};
    if (auth) {
      final token = await _getToken();
      if (token != null) {
        headers['Authorization'] = 'Bearer $token';
      }
    }
    return headers;
  }

  /// İsteği gönderir; erişim token'ı süresi dolduysa bir kez yenileyip tekrar dener.
  static Future<http.Response> _send(
    Future<http.Response> Function(Map<String, String> headers) request, {
    required bool auth,
    Duration? timeout,
  }) async {
    final limit = timeout ?? _timeout;
    var response = await request(await _headers(auth: auth)).timeout(limit);
    if (!auth || response.statusCode != 401) return response;

    if (await _refreshTokens()) {
      response = await request(await _headers(auth: auth)).timeout(limit);
      if (response.statusCode != 401) return response;
    }

    onSessionExpired?.call();
    return response;
  }

  /// Aynı anda gelen 401'ler tek bir yenileme isteğini paylaşır.
  static Future<bool> _refreshTokens() {
    return _refreshing ??= _doRefresh().whenComplete(() => _refreshing = null);
  }

  static Future<bool> _doRefresh() async {
    final prefs = await SharedPreferences.getInstance();
    final refreshToken = prefs.getString('refreshToken');
    if (refreshToken == null) return false;
    try {
      final response = await http
          .post(
            Uri.parse('$baseUrl/auth/refresh-token'),
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({'refreshToken': refreshToken}),
          )
          .timeout(_timeout);
      if (response.statusCode != 200) return false;
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      await prefs.setString('accessToken', data['accessToken']);
      await prefs.setString('refreshToken', data['refreshToken']);
      return true;
    } catch (_) {
      return false;
    }
  }

  // ── GET ──
  static Future<http.Response> get(String path, {bool auth = true, Duration? timeout}) {
    return _send(
      (h) => http.get(Uri.parse('$baseUrl$path'), headers: h),
      auth: auth,
      timeout: timeout,
    );
  }

  // ── POST ──
  static Future<http.Response> post(
    String path,
    Map<String, dynamic> body, {
    bool auth = true,
    Duration? timeout,
  }) {
    return _send(
      (h) => http.post(
        Uri.parse('$baseUrl$path'),
        headers: h,
        body: jsonEncode(body),
      ),
      auth: auth,
      timeout: timeout,
    );
  }

  // ── POST multipart (dosya yükleme) ──
  static Future<http.Response> postFile(
    String path, {
    required List<int> bytes,
    required String fileName,
    String field = 'file',
    Duration? timeout,
  }) {
    return _send((h) async {
      final request = http.MultipartRequest('POST', Uri.parse('$baseUrl$path'));
      if (h['Authorization'] != null) request.headers['Authorization'] = h['Authorization']!;
      request.files.add(http.MultipartFile.fromBytes(field, bytes, filename: fileName));
      return http.Response.fromStream(await request.send());
    }, auth: true, timeout: timeout);
  }

  // ── PUT ──
  static Future<http.Response> put(
    String path,
    Map<String, dynamic> body, {
    bool auth = true,
  }) {
    return _send(
      (h) => http.put(
        Uri.parse('$baseUrl$path'),
        headers: h,
        body: jsonEncode(body),
      ),
      auth: auth,
    );
  }

  // ── DELETE ──
  static Future<http.Response> delete(String path, {bool auth = true}) {
    return _send(
      (h) => http.delete(Uri.parse('$baseUrl$path'), headers: h),
      auth: auth,
    );
  }

  static bool isOk(http.Response r) => r.statusCode >= 200 && r.statusCode < 300;

  /// Backend'in { "error": "..." } gövdesinden kullanıcıya gösterilecek mesajı çıkarır.
  static String errorOf(http.Response r, [String fallback = 'İşlem başarısız']) {
    if (r.statusCode == 403) return 'Bu işlem için yetkiniz yok.';
    if (r.statusCode == 429) return 'Çok fazla deneme yapıldı. Biraz bekleyin.';
    try {
      final body = jsonDecode(r.body);
      if (body is Map && body['error'] is String) return body['error'];
    } catch (_) {}
    return '$fallback (${r.statusCode})';
  }

  static dynamic decode(http.Response r) =>
      r.body.isEmpty ? null : jsonDecode(r.body);
}

/// Servis çağrılarının ortak sonucu: başarılıysa [data], değilse [error].
class ApiResult<T> {
  const ApiResult.ok(this.data) : error = null;
  const ApiResult.fail(this.error) : data = null;

  final T? data;
  final String? error;

  bool get isOk => error == null;
}
