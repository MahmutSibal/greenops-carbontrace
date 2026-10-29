import 'dart:convert';
import 'api_client.dart';

class FacilityService {
  static Future<List<Map<String, dynamic>>> getAll({String? companyId}) async {
    final res = await list(companyId: companyId);
    return res.data ?? [];
  }

  static Future<ApiResult<List<Map<String, dynamic>>>> list({String? companyId}) async {
    final query = companyId != null ? '?companyId=$companyId' : '';
    final r = await ApiClient.get('/facilities$query');
    if (!ApiClient.isOk(r)) return ApiResult.fail(ApiClient.errorOf(r, 'Tesisler yüklenemedi'));
    return ApiResult.ok(List<Map<String, dynamic>>.from(jsonDecode(r.body)));
  }

  static Future<ApiResult<void>> create({
    required String companyId,
    required String name,
    String? address,
    String? department,
    int? employeeCount,
    double? squareMeters,
  }) async {
    final r = await ApiClient.post('/facilities', {
      'companyId': companyId,
      'name': name,
      if (address != null && address.isNotEmpty) 'address': address,
      if (department != null && department.isNotEmpty) 'department': department,
      'employeeCount': ?employeeCount,
      'squareMeters': ?squareMeters,
    });
    // Backend 201 Created döner; önceden sadece 200 kabul edildiği için her ekleme "başarısız" görünüyordu.
    return ApiClient.isOk(r) ? const ApiResult.ok(null) : ApiResult.fail(ApiClient.errorOf(r, 'Tesis eklenemedi'));
  }

  static Future<ApiResult<void>> delete(String id) async {
    final r = await ApiClient.delete('/facilities/$id');
    return ApiClient.isOk(r) ? const ApiResult.ok(null) : ApiResult.fail(ApiClient.errorOf(r));
  }
}

class CategoryService {
  static Future<List<Map<String, dynamic>>> getAll() async {
    final response = await ApiClient.get('/categories');
    if (response.statusCode == 200) {
      return List<Map<String, dynamic>>.from(jsonDecode(response.body));
    }
    return [];
  }
}
