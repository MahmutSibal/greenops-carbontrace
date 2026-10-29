import 'api_client.dart';

class EmissionService {
  static Future<ApiResult<Map<String, dynamic>>> list({
    String? facilityId,
    String? categoryId,
    int page = 1,
    int pageSize = 50,
  }) async {
    final params = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      'facilityId': ?facilityId,
      'categoryId': ?categoryId,
    };
    final r = await ApiClient.get('/emissions?${Uri(queryParameters: params).query}');
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Kayıtlar yüklenemedi'));
    return ApiResult.ok(Map<String, dynamic>.from(ApiClient.decode(r)));
  }

  /// Geriye dönük uyumluluk: sadece ilk sayfanın kayıtları.
  static Future<List<Map<String, dynamic>>> getHistory({String? facilityId, String? categoryId}) async {
    final res = await list(facilityId: facilityId, categoryId: categoryId);
    final items = res.data?['items'];
    return items is List ? List<Map<String, dynamic>>.from(items) : [];
  }

  static Future<ApiResult<Map<String, dynamic>>> detail(String id) async {
    final r = await ApiClient.get('/emissions/$id');
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r));
    return ApiResult.ok(Map<String, dynamic>.from(ApiClient.decode(r)));
  }

  static Future<ApiResult<String>> create({
    required String facilityId,
    required String categoryId,
    required double value,
    DateTime? periodStart,
    DateTime? periodEnd,
    String? evidenceUrl,
  }) async {
    final r = await ApiClient.post('/emissions/entry', {
      'facilityId': facilityId,
      'categoryId': categoryId,
      'value': value,
      if (periodStart != null) 'periodStart': _utcDate(periodStart),
      if (periodEnd != null) 'periodEnd': _utcDate(periodEnd),
      if (evidenceUrl != null && evidenceUrl.isNotEmpty) 'evidenceUrl': evidenceUrl,
    });
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Kayıt oluşturulamadı'));
    return ApiResult.ok(ApiClient.decode(r)['id']?.toString() ?? '');
  }

  static Future<ApiResult<void>> approve(String id) =>
      _action(() => ApiClient.put('/emissions/$id/approve', {}));

  static Future<ApiResult<void>> reject(String id, String reason) =>
      _action(() => ApiClient.put('/emissions/$id/reject', {'reason': reason}));

  static Future<ApiResult<void>> delete(String id) =>
      _action(() => ApiClient.delete('/emissions/$id'));

  static Future<ApiResult<void>> _action(Future<dynamic> Function() call) async {
    final r = await call();
    return ApiClient.isOk(r) ? const ApiResult.ok(null) : ApiResult.fail(ApiClient.errorOf(r));
  }

  // Takvim tarihi UTC gece yarısı olarak gönderilir (saat dilimi kayması olmasın).
  static String _utcDate(DateTime d) =>
      DateTime.utc(d.year, d.month, d.day).toIso8601String();
}
