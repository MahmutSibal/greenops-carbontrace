import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import 'api_client.dart';

enum UserRole { superAdmin, companyAdmin, manager, personnel }

extension UserRoleX on UserRole {
  static UserRole parse(String? value) => switch (value) {
    'SuperAdmin' => UserRole.superAdmin,
    'CompanyAdmin' => UserRole.companyAdmin,
    'Manager' => UserRole.manager,
    _ => UserRole.personnel,
  };

  String get label => switch (this) {
    UserRole.superAdmin => 'Süper Yönetici',
    UserRole.companyAdmin => 'Şirket Yöneticisi',
    UserRole.manager => 'Yönetici',
    UserRole.personnel => 'Personel',
  };

  /// Backend sıralaması: SuperAdmin=0 … Personnel=3.
  bool atLeast(UserRole other) => index <= other.index;
}

class AuthService {
  static const _connectionError =
      'API bağlantısı kurulamadı. Backend (http://localhost:5223) çalışıyor mu kontrol edin.';

  /// Dönen map: success, error, twoFactorRequired.
  static Future<Map<String, dynamic>> login(
    String email,
    String password, {
    String? twoFactorCode,
  }) async {
    try {
      final response = await ApiClient.post('/auth/login', {
        'email': email,
        'password': password,
        if (twoFactorCode != null && twoFactorCode.isNotEmpty)
          'twoFactorCode': twoFactorCode,
      }, auth: false);

      if (response.statusCode == 200) {
        await _persistSession(jsonDecode(response.body));
        return {'success': true};
      }

      Map<String, dynamic> body = {};
      try {
        body = jsonDecode(response.body);
      } catch (_) {}
      return {
        'success': false,
        'twoFactorRequired': body['twoFactorRequired'] == true,
        'error': ApiClient.errorOf(response, 'Giriş başarısız'),
      };
    } catch (_) {
      return {'success': false, 'error': _connectionError};
    }
  }

  static Future<Map<String, dynamic>> register(
    String fullName,
    String email,
    String password,
  ) async {
    try {
      final response = await ApiClient.post('/auth/register', {
        'fullName': fullName,
        'email': email,
        'password': password,
      }, auth: false);

      if (response.statusCode == 200) {
        await _persistSession(jsonDecode(response.body));
        return {'success': true};
      }
      return {
        'success': false,
        'error': ApiClient.errorOf(response, 'Kayıt başarısız'),
      };
    } catch (_) {
      return {'success': false, 'error': _connectionError};
    }
  }

  static Future<void> _persistSession(Map<String, dynamic> data) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('accessToken', data['accessToken']);
    await prefs.setString('refreshToken', data['refreshToken']);
    await prefs.setString('userEmail', data['email'] ?? '');
    await prefs.setString('userFullName', data['fullName'] ?? '');
    await prefs.setString('userRole', data['role'] ?? '');
    if (data['companyId'] != null) {
      await prefs.setString('companyId', data['companyId']);
    } else {
      await prefs.remove('companyId');
    }
  }

  static Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    final refreshToken = prefs.getString('refreshToken');
    // Sunucudaki refresh token'ı da iptal et (ağ hatası çıkışı engellemez).
    if (refreshToken != null) {
      try {
        await ApiClient.post('/auth/logout', {'refreshToken': refreshToken});
      } catch (_) {}
    }
    await clearLocalSession();
  }

  static Future<void> clearLocalSession() async {
    final prefs = await SharedPreferences.getInstance();
    for (final key in [
      'accessToken',
      'refreshToken',
      'userEmail',
      'userFullName',
      'userRole',
      'companyId',
    ]) {
      await prefs.remove(key);
    }
  }

  static Future<bool> isLoggedIn() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('accessToken') != null;
  }

  static Future<String?> getUserEmail() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('userEmail');
  }

  static Future<String?> getFullName() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('userFullName');
  }

  static Future<String?> getUserRole() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('userRole');
  }

  static Future<UserRole> getRole() async => UserRoleX.parse(await getUserRole());

  static Future<ApiResult<void>> changePassword(
    String currentPassword,
    String newPassword,
  ) async {
    final r = await ApiClient.put('/users/me/change-password', {
      'currentPassword': currentPassword,
      'newPassword': newPassword,
    });
    return ApiClient.isOk(r)
        ? const ApiResult.ok(null)
        : ApiResult.fail(ApiClient.errorOf(r));
  }

  static Future<String?> getCompanyId() async {
    final prefs = await SharedPreferences.getInstance();
    final cached = prefs.getString('companyId');
    if (cached != null && cached.isNotEmpty) {
      return cached;
    }

    // Fallback 1: access token icindeki companyId claim'i (varsa)
    final token = prefs.getString('accessToken');
    if (token != null) {
      final fromToken = _extractCompanyIdFromToken(token);
      if (fromToken != null && fromToken.isNotEmpty) {
        await prefs.setString('companyId', fromToken);
        return fromToken;
      }
    }

    // Fallback 2: SuperAdmin gibi companyId'siz oturumlar icin ilk sirketi sec
    try {
      final companiesRes = await ApiClient.get('/companies');
      if (companiesRes.statusCode == 200) {
        final decoded = jsonDecode(companiesRes.body);
        if (decoded is List && decoded.isNotEmpty) {
          final first = decoded.first;
          if (first is Map<String, dynamic>) {
            final id = (first['id'] ?? first['Id'])?.toString();
            if (id != null && id.isNotEmpty) {
              await prefs.setString('companyId', id);
              return id;
            }
          }
        }
      }
    } catch (_) {
      // Network veya parse hatasinda sessizce null don
    }

    return null;
  }

  static String? _extractCompanyIdFromToken(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return null;

      final payload = jsonDecode(
        utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))),
      );

      if (payload is! Map<String, dynamic>) return null;
      final companyId = payload['companyId'] ?? payload['CompanyId'];
      return companyId?.toString();
    } catch (_) {
      return null;
    }
  }
}
