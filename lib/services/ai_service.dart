import 'api_client.dart';

/// Backend'in yerel Ollama modelleri üzerinden sunduğu AI uçları.
/// Yerel model yanıtları uzun sürebildiği için zaman aşımları geniş tutulur.
class AiService {
  static const _long = Duration(seconds: 240);

  static Future<ApiResult<Map<String, dynamic>>> status() async {
    final r = await ApiClient.get('/ai/status');
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r));
    return ApiResult.ok(Map<String, dynamic>.from(ApiClient.decode(r)));
  }

  static Future<ApiResult<Map<String, dynamic>>> insights({String? companyId, bool refresh = false}) async {
    final params = <String>[
      if (companyId != null) 'companyId=$companyId',
      if (refresh) 'refresh=true',
    ];
    final r = await ApiClient.get(
      '/ai/insights${params.isEmpty ? '' : '?${params.join('&')}'}',
      timeout: _long,
    );
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Öngörüler alınamadı'));
    return ApiResult.ok(Map<String, dynamic>.from(ApiClient.decode(r)));
  }

  /// Fatura/sayaç fotoğrafını okur. Dönen alanlar sadece öneridir; kullanıcı onaylayıp kaydeder.
  static Future<ApiResult<Map<String, dynamic>>> scanInvoice(List<int> bytes, String fileName) async {
    final r = await ApiClient.postFile('/ai/invoice-scan', bytes: bytes, fileName: fileName, timeout: _long);
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Fatura okunamadı'));
    return ApiResult.ok(Map<String, dynamic>.from(ApiClient.decode(r)));
  }

  static Future<ApiResult<String>> chat(String message, List<Map<String, String>> history) async {
    final r = await ApiClient.post('/ai/chat', {'message': message, 'history': history}, timeout: _long);
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Asistan yanıt veremedi'));
    return ApiResult.ok(ApiClient.decode(r)['reply']?.toString() ?? '');
  }
}
