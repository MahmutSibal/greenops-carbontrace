import 'dart:convert';
import 'api_client.dart';

class DashboardService {
  static Future<Map<String, dynamic>> getSummary({
    required String companyId,
  }) async {
    final response = await ApiClient.get('/dashboard/kpi?companyId=$companyId');
    if (response.statusCode == 200) {
      return Map<String, dynamic>.from(jsonDecode(response.body));
    }
    throw Exception(ApiClient.errorOf(response));
  }

  static Future<List<Map<String, dynamic>>> getFacilityComparison({
    required String companyId,
  }) async {
    final response = await ApiClient.get(
      '/dashboard/facility-comparison?companyId=$companyId',
    );
    if (response.statusCode == 200) {
      return List<Map<String, dynamic>>.from(jsonDecode(response.body));
    }
    return [];
  }

  static Future<List<Map<String, dynamic>>> getTrends({
    required String companyId,
    int? year,
  }) async {
    final response = await ApiClient.get(
      '/dashboard/trends?companyId=$companyId${year == null ? '' : '&year=$year'}',
    );
    if (response.statusCode == 200) {
      return List<Map<String, dynamic>>.from(jsonDecode(response.body));
    }
    return [];
  }
}
