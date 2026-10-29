import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../core/formatters.dart';
import '../services/ai_service.dart';
import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../services/company_services.dart';
import '../services/dashboard_service.dart';
import '../theme/app_theme.dart';
import '../widgets/aurora_background.dart';
import '../widgets/ui.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key, this.onOpenEmissions, this.onOpenAssistant});

  final VoidCallback? onOpenEmissions;
  final VoidCallback? onOpenAssistant;

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  static const _scopeColors = [AppColors.warning, AppColors.accent, Color(0xFFB18CFF)];

  Map<String, dynamic>? _summary;
  List<Map<String, dynamic>> _trends = [];
  List<Map<String, dynamic>> _facilities = [];
  Map<String, dynamic>? _progress;
  Map<String, dynamic>? _insights;
  String? _insightError;
  bool _insightLoading = false;
  bool _loading = true;
  String? _error;
  String _name = '';
  UserRole _role = UserRole.personnel;
  String? _companyId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final name = await AuthService.getFullName();
      final role = await AuthService.getRole();
      final companyId = await AuthService.getCompanyId();
      if (companyId == null) {
        if (!mounted) return;
        setState(() {
          _error = 'Şirket bilgisi bulunamadı.';
          _loading = false;
        });
        return;
      }

      final results = await Future.wait([
        DashboardService.getSummary(companyId: companyId),
        DashboardService.getTrends(companyId: companyId),
        DashboardService.getFacilityComparison(companyId: companyId),
        CompanyServices.targetProgress(companyId),
      ]);

      if (!mounted) return;
      setState(() {
        _name = (name ?? '').split(' ').first;
        _role = role;
        _companyId = companyId;
        _summary = results[0] as Map<String, dynamic>;
        _trends = results[1] as List<Map<String, dynamic>>;
        _facilities = (results[2] as List<Map<String, dynamic>>)
          ..sort((a, b) => ((b['totalEmissions'] ?? 0) as num).compareTo((a['totalEmissions'] ?? 0) as num));
        _progress = (results[3] as ApiResult<Map<String, dynamic>>).data;
        _loading = false;
      });
      _loadInsights();
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Veriler yüklenemedi. Bağlantınızı kontrol edin.';
        _loading = false;
      });
    }
  }

  /// AI öngörüleri ayrıca yüklenir; yerel model yavaşsa panelin geri kalanı beklemez.
  Future<void> _loadInsights({bool refresh = false}) async {
    setState(() {
      _insightLoading = true;
      _insightError = null;
    });
    final res = await AiService.insights(companyId: _role == UserRole.superAdmin ? _companyId : null, refresh: refresh);
    if (!mounted) return;
    setState(() {
      _insightLoading = false;
      _insights = res.data ?? _insights;
      _insightError = res.error;
    });
  }

  String get _greeting {
    final h = DateTime.now().hour;
    if (h < 6) return 'İyi geceler';
    if (h < 12) return 'Günaydın';
    if (h < 18) return 'İyi günler';
    return 'İyi akşamlar';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: AuroraBackground(
        child: _loading
            ? _buildSkeleton()
            : _error != null
            ? Center(child: ErrorState(message: _error!, onRetry: _load))
            : RefreshIndicator(
                onRefresh: _load,
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 110),
                  children: [
                    _buildHeader(),
                    const SizedBox(height: 16),
                    _buildHero(),
                    const SizedBox(height: 12),
                    _buildKpis(),
                    const SizedBox(height: 20),
                    _buildInsights(),
                    const SizedBox(height: 20),
                    _buildTrend(),
                    const SizedBox(height: 20),
                    _buildScopes(),
                    if (_facilities.isNotEmpty) ...[const SizedBox(height: 20), _buildFacilities()],
                    if (_progress != null && ((_progress!['baselineEmissions'] ?? 0) as num) > 0) ...[
                      const SizedBox(height: 20),
                      _buildTarget(),
                    ],
                  ],
                ),
              ),
      ),
    );
  }

  Widget _buildSkeleton() => ListView(
    padding: const EdgeInsets.all(16),
    physics: const NeverScrollableScrollPhysics(),
    children: const [
      Skeleton(height: 54, width: 220),
      SizedBox(height: 16),
      Skeleton(height: 150, radius: 24),
      SizedBox(height: 12),
      Row(children: [
        Expanded(child: Skeleton(height: 86, radius: 18)),
        SizedBox(width: 10),
        Expanded(child: Skeleton(height: 86, radius: 18)),
        SizedBox(width: 10),
        Expanded(child: Skeleton(height: 86, radius: 18)),
      ]),
      SizedBox(height: 20),
      Skeleton(height: 180, radius: 20),
      SizedBox(height: 20),
      Skeleton(height: 220, radius: 20),
    ],
  );

  Widget _buildHeader() {
    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '$_greeting${_name.isEmpty ? '' : ', $_name'}',
                style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 4),
              Row(
                children: [
                  StatusChip(label: _role.label, color: AppColors.primary, icon: Icons.verified_user_outlined),
                  const SizedBox(width: 8),
                  Text(Fmt.monthYear(DateTime.now()), style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                ],
              ),
            ],
          ),
        ),
        IconButton.filledTonal(
          onPressed: widget.onOpenAssistant,
          tooltip: 'AI Asistan',
          style: IconButton.styleFrom(backgroundColor: AppColors.primary.withValues(alpha: 0.15)),
          icon: const Icon(Icons.auto_awesome_rounded, color: AppColors.primary),
        ),
      ],
    );
  }

  Widget _buildHero() {
    final total = (_summary?['totalEmissions'] ?? 0) as num;
    final change = (_summary?['monthlyChange'] ?? 0) as num;
    final up = change > 0;
    return GlassCard(
      padding: const EdgeInsets.all(20),
      gradient: LinearGradient(
        colors: [AppColors.primary.withValues(alpha: 0.22), AppColors.accent.withValues(alpha: 0.10), AppColors.surface],
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
      ),
      borderColor: AppColors.primary.withValues(alpha: 0.35),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.cloud_outlined, size: 18, color: AppColors.textMuted),
              SizedBox(width: 6),
              Text('Toplam onaylı emisyon', style: TextStyle(color: AppColors.textMuted, fontSize: 13)),
            ],
          ),
          const SizedBox(height: 10),
          Text(Fmt.co2(total), style: const TextStyle(fontSize: 32, fontWeight: FontWeight.w800, letterSpacing: -0.5)),
          const SizedBox(height: 12),
          Row(
            children: [
              StatusChip(
                label: '${Fmt.percent(change, signed: true)} son ay, öncekine göre',
                color: change == 0 ? AppColors.textMuted : (up ? AppColors.danger : AppColors.primary),
                icon: change == 0 ? Icons.remove : (up ? Icons.trending_up_rounded : Icons.trending_down_rounded),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildKpis() {
    final pending = (_summary?['pendingEntries'] ?? 0) as num;
    return Row(
      children: [
        Expanded(child: _kpi(Icons.apartment_rounded, '${_summary?['facilityCount'] ?? 0}', 'Aktif tesis', AppColors.accent)),
        const SizedBox(width: 10),
        Expanded(
          child: _kpi(Icons.pending_actions_rounded, '$pending', 'Onay bekliyor', AppColors.warning,
              onTap: pending > 0 ? widget.onOpenEmissions : null),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _kpi(Icons.groups_rounded, Fmt.co2Axis((_summary?['carbonIntensity'] ?? 0) as num), 'kg / çalışan',
              const Color(0xFF8FD14F)),
        ),
      ],
    );
  }

  Widget _kpi(IconData icon, String value, String label, Color color, {VoidCallback? onTap}) {
    return GlassCard(
      onTap: onTap,
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          IconBadge(icon: icon, color: color, size: 34),
          const SizedBox(height: 10),
          Text(value, maxLines: 1, overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
          Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 11)),
        ],
      ),
    );
  }

  // ── AI öngörüleri ──
  Widget _buildInsights() {
    final ins = _insights;
    final recs = (ins?['recommendations'] as List?)?.cast<String>() ?? const [];
    final anomalies = (ins?['anomalies'] as List?) ?? const [];
    final forecast = ins?['forecastNextMonth'] as num?;
    final trend = ins?['trend'] as String? ?? 'stable';

    return GlassCard(
      padding: const EdgeInsets.all(18),
      borderColor: const Color(0xFFB18CFF).withValues(alpha: 0.4),
      gradient: LinearGradient(
        colors: [const Color(0xFFB18CFF).withValues(alpha: 0.14), AppColors.surface.withValues(alpha: 0.92)],
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const IconBadge(icon: Icons.auto_awesome_rounded, color: Color(0xFFB18CFF), size: 36),
              const SizedBox(width: 10),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('AI Öngörüleri', style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
                    Text('gemma4 · yerel model', style: TextStyle(fontSize: 11, color: AppColors.textMuted)),
                  ],
                ),
              ),
              IconButton(
                tooltip: 'Yeniden analiz et',
                onPressed: _insightLoading ? null : () => _loadInsights(refresh: true),
                icon: _insightLoading
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.refresh_rounded, color: AppColors.textMuted),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (_insightLoading && ins == null) ...[
            const Text('Verileriniz analiz ediliyor…', style: TextStyle(color: AppColors.textMuted, fontSize: 13)),
            const SizedBox(height: 10),
            const Skeleton(height: 12),
            const SizedBox(height: 8),
            const Skeleton(height: 12, width: 220),
          ] else if (ins == null) ...[
            Text(_insightError ?? 'Öngörü alınamadı.', style: const TextStyle(color: AppColors.textMuted, fontSize: 13)),
          ] else ...[
            Text(ins['summary']?.toString() ?? '', style: const TextStyle(fontSize: 13.5, height: 1.45)),
            if (forecast != null) ...[
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(
                    child: _miniStat(
                      'Gelecek ay tahmini',
                      Fmt.co2(forecast),
                      trend == 'up' ? Icons.north_east_rounded : trend == 'down' ? Icons.south_east_rounded : Icons.east_rounded,
                      trend == 'up' ? AppColors.danger : trend == 'down' ? AppColors.primary : AppColors.accent,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _miniStat(
                      'Şüpheli kayıt',
                      '${anomalies.length}',
                      Icons.report_problem_outlined,
                      anomalies.isEmpty ? AppColors.primary : AppColors.warning,
                      onTap: anomalies.isEmpty ? null : () => _showAnomalies(anomalies),
                    ),
                  ),
                ],
              ),
            ],
            if (recs.isNotEmpty) ...[
              const SizedBox(height: 14),
              const Text('Öneriler', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w800, color: AppColors.textMuted)),
              const SizedBox(height: 6),
              for (final (i, r) in recs.indexed)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        width: 20,
                        height: 20,
                        margin: const EdgeInsets.only(top: 1),
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.16),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text('${i + 1}', style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppColors.primary)),
                      ),
                      const SizedBox(width: 10),
                      Expanded(child: Text(r, style: const TextStyle(fontSize: 13, height: 1.4))),
                    ],
                  ),
                ),
            ],
            if (_insightError != null)
              Text(_insightError!, style: const TextStyle(color: AppColors.warning, fontSize: 11.5)),
            const SizedBox(height: 4),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton.icon(
                onPressed: widget.onOpenAssistant,
                icon: const Icon(Icons.chat_bubble_outline_rounded, size: 16, color: AppColors.primary),
                label: const Text('Asistana sor', style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w700)),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _miniStat(String label, String value, IconData icon, Color color, {VoidCallback? onTap}) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: AppColors.background.withValues(alpha: 0.5),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.border),
        ),
        child: Row(
          children: [
            Icon(icon, color: color, size: 20),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(value, maxLines: 1, overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14)),
                  Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 10.5)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showAnomalies(List anomalies) {
    showAppSheet(
      context,
      title: 'Şüpheli kayıtlar',
      builder: (ctx) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Bu bekleyen kayıtlar, aynı kategorideki onaylı kayıtlardan belirgin şekilde yüksek. Onaylamadan önce kontrol edin.',
            style: TextStyle(color: AppColors.textMuted, fontSize: 12.5, height: 1.4),
          ),
          const SizedBox(height: 14),
          for (final a in anomalies)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: GlassCard(
                padding: const EdgeInsets.all(14),
                borderColor: AppColors.warning.withValues(alpha: 0.4),
                child: Row(
                  children: [
                    const IconBadge(icon: Icons.report_problem_outlined, color: AppColors.warning),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('${a['facilityName']} · ${a['categoryName']}', style: const TextStyle(fontWeight: FontWeight.w700)),
                          const SizedBox(height: 2),
                          Text(a['reason']?.toString() ?? '', style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          const SizedBox(height: 6),
          FilledButton.icon(
            onPressed: () {
              Navigator.pop(ctx);
              widget.onOpenEmissions?.call();
            },
            icon: const Icon(Icons.fact_check_outlined),
            label: const Text('Kayıtları incele'),
          ),
        ],
      ),
    );
  }

  // ── Aylık trend (bar) + tahmin ──
  Widget _buildTrend() {
    final points = _trends.length > 12 ? _trends.sublist(_trends.length - 12) : _trends;
    final forecast = _insights?['forecastNextMonth'] as num?;
    final forecastMonth = _insights?['forecastMonth'] as String?;

    if (points.isEmpty) {
      return const GlassCard(
        child: EmptyState(
          icon: Icons.bar_chart_rounded,
          title: 'Henüz trend verisi yok',
          message: 'Emisyon kayıtları onaylandıkça aylık trend burada görünecek.',
        ),
      );
    }

    final values = points.map((p) => ((p['totalEmissions'] ?? 0) as num).toDouble()).toList();
    final showForecast = forecast != null && forecastMonth != null;
    final maxY = [...values, if (showForecast) forecast.toDouble()].reduce((a, b) => a > b ? a : b) * 1.2;

    String label(int i) {
      if (i < points.length) return Fmt.monthShort(points[i]['year'], points[i]['month']);
      final parts = forecastMonth!.split('-');
      return Fmt.monthShort(int.parse(parts[0]), int.parse(parts[1]));
    }

    final groups = [
      for (final (i, v) in values.indexed)
        BarChartGroupData(x: i, barRods: [
          BarChartRodData(
            toY: v,
            width: 14,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(6)),
            gradient: const LinearGradient(colors: [AppColors.accent, AppColors.primary], begin: Alignment.bottomCenter, end: Alignment.topCenter),
          ),
        ]),
      if (showForecast)
        BarChartGroupData(x: values.length, barRods: [
          BarChartRodData(
            toY: forecast.toDouble(),
            width: 14,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(6)),
            color: const Color(0xFFB18CFF).withValues(alpha: 0.45),
            borderSide: const BorderSide(color: Color(0xFFB18CFF), width: 1),
          ),
        ]),
    ];

    return GlassCard(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionTitle(
            'Aylık emisyon',
            subtitle: 'Faaliyet dönemine göre, kgCO₂e',
            trailing: showForecast ? const StatusChip(label: 'Tahmin', color: Color(0xFFB18CFF)) : null,
          ),
          SizedBox(
            height: 190,
            child: BarChart(
              BarChartData(
                maxY: maxY,
                alignment: BarChartAlignment.spaceAround,
                barGroups: groups,
                gridData: FlGridData(
                  drawVerticalLine: false,
                  horizontalInterval: maxY / 4,
                  getDrawingHorizontalLine: (_) => const FlLine(color: AppColors.border, strokeWidth: 0.6, dashArray: [4, 4]),
                ),
                borderData: FlBorderData(show: false),
                barTouchData: BarTouchData(
                  touchTooltipData: BarTouchTooltipData(
                    getTooltipColor: (_) => AppColors.surfaceSoft,
                    getTooltipItem: (g, _, rod, _) => BarTooltipItem(
                      '${label(g.x)}\n${Fmt.co2(rod.toY)}${g.x >= points.length ? '\n(tahmin)' : ''}',
                      const TextStyle(color: AppColors.text, fontSize: 11, fontWeight: FontWeight.w700),
                    ),
                  ),
                ),
                titlesData: FlTitlesData(
                  topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  leftTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      reservedSize: 38,
                      interval: maxY / 4,
                      getTitlesWidget: (v, meta) => v == meta.max
                          ? const SizedBox.shrink()
                          : Text(Fmt.co2Axis(v), style: const TextStyle(color: AppColors.textMuted, fontSize: 9.5)),
                    ),
                  ),
                  bottomTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      getTitlesWidget: (v, _) => Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: Text(label(v.toInt()), style: const TextStyle(color: AppColors.textMuted, fontSize: 10)),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ── Kapsam dağılımı (halka) ──
  Widget _buildScopes() {
    final values = [
      ((_summary?['scope1Total'] ?? 0) as num).toDouble(),
      ((_summary?['scope2Total'] ?? 0) as num).toDouble(),
      ((_summary?['scope3Total'] ?? 0) as num).toDouble(),
    ];
    final total = values.fold<double>(0, (a, b) => a + b);
    const labels = ['Kapsam 1 · Doğrudan', 'Kapsam 2 · Enerji', 'Kapsam 3 · Değer zinciri'];

    return GlassCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionTitle('Kapsam dağılımı', subtitle: 'GHG Protokolü'),
          Row(
            children: [
              SizedBox(
                width: 130,
                height: 130,
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    PieChart(
                      PieChartData(
                        centerSpaceRadius: 42,
                        sectionsSpace: 3,
                        startDegreeOffset: -90,
                        sections: total == 0
                            ? [PieChartSectionData(value: 1, color: AppColors.border, radius: 16, showTitle: false)]
                            : [
                                for (final (i, v) in values.indexed)
                                  if (v > 0) PieChartSectionData(value: v, color: _scopeColors[i], radius: 16, showTitle: false),
                              ],
                      ),
                    ),
                    Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(Fmt.co2Axis(total), style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
                        const Text('kgCO₂e', style: TextStyle(color: AppColors.textMuted, fontSize: 9.5)),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  children: [
                    for (final (i, v) in values.indexed)
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 6),
                        child: Row(
                          children: [
                            Container(width: 10, height: 10, decoration: BoxDecoration(color: _scopeColors[i], borderRadius: BorderRadius.circular(3))),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(labels[i], style: const TextStyle(fontSize: 11.5, color: AppColors.textMuted)),
                                  Text(Fmt.co2(v), style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
                                ],
                              ),
                            ),
                            Text(Fmt.percent(total == 0 ? 0 : v / total * 100),
                                style: TextStyle(color: _scopeColors[i], fontSize: 12, fontWeight: FontWeight.w700)),
                          ],
                        ),
                      ),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // ── Tesis karşılaştırması ──
  Widget _buildFacilities() {
    final top = _facilities.take(5).toList();
    final max = top.map((f) => ((f['totalEmissions'] ?? 0) as num).toDouble()).fold<double>(0, (a, b) => a > b ? a : b);
    return GlassCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionTitle('Tesis karşılaştırması', subtitle: 'En yüksek emisyonlu tesisler'),
          for (final (i, f) in top.indexed) ...[
            Row(
              children: [
                Expanded(
                  child: Text('${i + 1}. ${f['facilityName']}', maxLines: 1, overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
                ),
                Text(Fmt.co2(f['totalEmissions'] as num?), style: const TextStyle(fontSize: 12, color: AppColors.textMuted)),
              ],
            ),
            const SizedBox(height: 6),
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: LinearProgressIndicator(
                value: max == 0 ? 0 : ((f['totalEmissions'] ?? 0) as num) / max,
                minHeight: 8,
                backgroundColor: AppColors.border.withValues(alpha: 0.6),
                valueColor: AlwaysStoppedAnimation(i == 0 ? AppColors.warning : AppColors.accent),
              ),
            ),
            const SizedBox(height: 12),
          ],
        ],
      ),
    );
  }

  // ── Hedef ilerlemesi ──
  Widget _buildTarget() {
    final p = _progress!;
    final pct = ((p['progressPercent'] ?? 0) as num).toDouble();
    return GlassCard(
      child: Row(
        children: [
          SizedBox(
            width: 64,
            height: 64,
            child: Stack(
              alignment: Alignment.center,
              children: [
                CircularProgressIndicator(
                  value: pct / 100,
                  strokeWidth: 7,
                  backgroundColor: AppColors.border,
                  valueColor: const AlwaysStoppedAnimation(AppColors.primary),
                ),
                Text('%${pct.round()}', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 11.5)),
              ],
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Azaltım hedefi', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15)),
                const SizedBox(height: 4),
                Text(
                  p['isProjected'] == true
                      ? 'Yıllık tahmin: ${Fmt.co2(p['projectedAnnualEmissions'] as num?)} (${p['monthsCovered']} aylık veriden)'
                      : 'Bu yıl: ${Fmt.co2(p['projectedAnnualEmissions'] as num?)}',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                ),
                Text('Yıllık hedef: ${Fmt.co2(p['targetEmissions'] as num?)}',
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
              ],
            ),
          ),
          IconButton(
            onPressed: () => Navigator.pushNamed(context, '/targets'),
            icon: const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
          ),
        ],
      ),
    );
  }
}
