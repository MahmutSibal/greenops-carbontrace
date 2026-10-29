import 'package:intl/intl.dart';

/// Backend emisyonları kgCO2e cinsinden döner; büyük değerler tona çevrilerek gösterilir.
class Fmt {
  static final _num1 = NumberFormat('#,##0.0', 'tr_TR');
  static final _num0 = NumberFormat('#,##0', 'tr_TR');
  static final _date = DateFormat('d MMM y', 'tr_TR');
  static final _dateTime = DateFormat('d MMM y HH:mm', 'tr_TR');
  static final _monthYear = DateFormat('MMMM y', 'tr_TR');
  static final _monthShort = DateFormat('MMM', 'tr_TR');

  static String co2(num? kg) {
    final v = (kg ?? 0).toDouble();
    if (v.abs() >= 1000) return '${_num1.format(v / 1000)} tCO₂e';
    return '${_num1.format(v)} kgCO₂e';
  }

  /// Kompakt eksen etiketi (ör. 1,2t / 850).
  static String co2Axis(num v) {
    if (v.abs() >= 1000) return '${_num1.format(v / 1000)}t';
    return _num0.format(v);
  }

  static String number(num? v) => _num1.format(v ?? 0);

  /// Emisyon katsayısı: anlamlı tüm ondalıklar, Türkçe biçim (0,469).
  static String factor(num? v) => v == null ? '?' : NumberFormat('#,##0.######', 'tr_TR').format(v);

  static String integer(num? v) => _num0.format(v ?? 0);

  static String percent(num? v, {bool signed = false}) {
    final d = (v ?? 0).toDouble();
    final s = '%${_num1.format(d.abs())}';
    if (!signed || d == 0) return s;
    return d > 0 ? '+$s' : '-$s';
  }

  static DateTime? parse(dynamic iso) =>
      iso is String ? DateTime.tryParse(iso)?.toLocal() : null;

  static String date(dynamic iso) {
    final d = parse(iso);
    return d == null ? '-' : _date.format(d);
  }

  static String dateTime(dynamic iso) {
    final d = parse(iso);
    return d == null ? '-' : _dateTime.format(d);
  }

  static String monthYear(DateTime d) => _monthYear.format(d);

  static String monthShort(int year, int month) =>
      _monthShort.format(DateTime(year, month));

  static String relative(dynamic iso) {
    final d = parse(iso);
    if (d == null) return '-';
    final diff = DateTime.now().difference(d);
    if (diff.inMinutes < 1) return 'şimdi';
    if (diff.inMinutes < 60) return '${diff.inMinutes} dk önce';
    if (diff.inHours < 24) return '${diff.inHours} sa önce';
    if (diff.inDays < 7) return '${diff.inDays} gün önce';
    return _date.format(d);
  }
}
