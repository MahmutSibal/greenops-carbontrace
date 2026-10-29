import 'api_client.dart';

/// Hedef, kullanıcı, tedarikçi, bildirim ve denetim uçları.
/// Şirket kimliği backend'de token'dan çözülür; companyId sadece SuperAdmin için anlamlıdır.
class CompanyServices {
  static String _q(String? companyId) => companyId == null ? '' : '?companyId=$companyId';

  static Future<ApiResult<List<Map<String, dynamic>>>> _list(String path, String fallback) async {
    final r = await ApiClient.get(path);
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, fallback));
    final body = ApiClient.decode(r);
    return ApiResult.ok(body is List ? List<Map<String, dynamic>>.from(body) : []);
  }

  static Future<ApiResult<Map<String, dynamic>>> _map(String path, String fallback) async {
    final r = await ApiClient.get(path);
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, fallback));
    return ApiResult.ok(Map<String, dynamic>.from(ApiClient.decode(r)));
  }

  static Future<ApiResult<void>> _send(Future<dynamic> Function() call, String fallback) async {
    final r = await call();
    return ApiClient.isOk(r) ? const ApiResult.ok(null) : ApiResult.fail(ApiClient.errorOf(r, fallback));
  }

  // ── Hedefler ──
  static Future<ApiResult<List<Map<String, dynamic>>>> targets(String? companyId) =>
      _list('/targets${_q(companyId)}', 'Hedefler yüklenemedi');

  static Future<ApiResult<Map<String, dynamic>>> targetProgress(String? companyId) =>
      _map('/targets/progress${_q(companyId)}', 'İlerleme yüklenemedi');

  static Future<ApiResult<void>> createTarget({
    required String? companyId,
    required int targetYear,
    required int baselineYear,
    required double baselineEmissions,
    required double reductionPercent,
    String? description,
  }) =>
      _send(
        () => ApiClient.post('/targets', {
          'companyId': ?companyId,
          'targetYear': targetYear,
          'baselineYear': baselineYear,
          'baselineEmissions': baselineEmissions,
          'reductionPercent': reductionPercent,
          if (description != null && description.isNotEmpty) 'description': description,
        }),
        'Hedef oluşturulamadı',
      );

  /// Kategori bazlı azaltım senaryosu: {categoryId: yüzde}. Boş harita mevcut durumu (yıllık) döndürür.
  static Future<ApiResult<Map<String, dynamic>>> scenario(String? companyId, Map<String, double> measures) async {
    final r = await ApiClient.post('/targets/scenario', {
      'companyId': ?companyId,
      'measures': [
        for (final e in measures.entries) {'categoryId': e.key, 'reductionPercent': e.value},
      ],
    });
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Senaryo hesaplanamadı'));
    return ApiResult.ok(Map<String, dynamic>.from(ApiClient.decode(r)));
  }

  // ── Raporlar ──
  /// Yıllık envanter dosyası (format: pdf | xlsx). Başarılıysa dosya baytları.
  static Future<ApiResult<List<int>>> inventoryReport(String? companyId, int year, String format) async {
    final r = await ApiClient.get(
      '/reports/inventory?year=$year&format=$format${companyId == null ? '' : '&companyId=$companyId'}',
      timeout: const Duration(seconds: 60),
    );
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Rapor oluşturulamadı'));
    return ApiResult.ok(r.bodyBytes);
  }

  // ── Kullanıcılar ──
  static Future<ApiResult<List<Map<String, dynamic>>>> users(String? companyId) =>
      _list('/users${_q(companyId)}', 'Kullanıcılar yüklenemedi');

  static Future<ApiResult<void>> inviteUser({
    required String? companyId,
    required String fullName,
    required String email,
    required String role,
    required String password,
  }) =>
      _send(
        () => ApiClient.post('/users/invite', {
          'companyId': ?companyId,
          'fullName': fullName,
          'email': email,
          'role': role,
          'password': password,
        }),
        'Kullanıcı eklenemedi',
      );

  static Future<ApiResult<void>> setUserStatus(String userId, bool isActive) =>
      _send(() => ApiClient.put('/users/$userId/status', {'isActive': isActive}), 'Durum değiştirilemedi');

  static Future<ApiResult<void>> setUserRole(String userId, String role) =>
      _send(() => ApiClient.put('/users/$userId/set-role', {'role': role}), 'Rol değiştirilemedi');

  // ── Tedarikçiler ──
  static Future<ApiResult<List<Map<String, dynamic>>>> suppliers(String? companyId) =>
      _list('/suppliers${_q(companyId)}', 'Tedarikçiler yüklenemedi');

  static Future<ApiResult<void>> addSupplier({
    required String? companyId,
    required String name,
    required String email,
  }) =>
      _send(
        () => ApiClient.post('/suppliers/invite', {'companyId': ?companyId, 'name': name, 'contactEmail': email}),
        'Tedarikçi eklenemedi',
      );

  static Future<ApiResult<void>> setEsgScore(String supplierId, int score) =>
      _send(() => ApiClient.put('/suppliers/$supplierId/esg-score', {'score': score}), 'Puan güncellenemedi');

  // ── Bildirimler ──
  static Future<ApiResult<List<Map<String, dynamic>>>> notifications() =>
      _list('/notifications', 'Bildirimler yüklenemedi');

  static Future<ApiResult<void>> markRead(String id) =>
      _send(() => ApiClient.put('/notifications/$id/read', {}), 'İşaretlenemedi');

  // ── Denetim kaydı ──
  static Future<ApiResult<Map<String, dynamic>>> auditLogs({String? companyId, int page = 1}) =>
      _map('/audit-logs?page=$page&pageSize=50${companyId == null ? '' : '&companyId=$companyId'}',
          'Denetim kaydı yüklenemedi');

  // ── Profil ──
  static Future<ApiResult<Map<String, dynamic>>> me() => _map('/users/me', 'Profil yüklenemedi');
}
