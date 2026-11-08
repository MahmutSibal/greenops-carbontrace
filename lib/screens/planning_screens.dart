import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:share_plus/share_plus.dart';

import '../core/formatters.dart';
import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../services/company_services.dart';
import '../theme/app_theme.dart';
import '../widgets/ui.dart';

Future<String?> _superAdminCompany() async =>
    await AuthService.getRole() == UserRole.superAdmin
    ? AuthService.getCompanyId()
    : null;

// ───────────────────────────── Azaltım senaryosu ─────────────────────────────

/// "Elektrik tüketimini %15 azaltırsak yılda kaç tCO₂e düşer, hedefe yeter mi?"
/// Yüzdeyi kullanıcı girer (enerji etüdü, tedarikçi teklifi vb.); uygulama önlem başına tasarruf oranı uydurmaz.
class ScenarioScreen extends StatefulWidget {
  const ScenarioScreen({super.key});

  @override
  State<ScenarioScreen> createState() => _ScenarioScreenState();
}

class _ScenarioScreenState extends State<ScenarioScreen> {
  final Map<String, double> _percents = {};
  Map<String, dynamic>? _result;
  bool _loading = true;
  String? _error;
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    _calculate();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  Future<void> _calculate() async {
    final r = await CompanyServices.scenario(await _superAdminCompany(), {
      for (final e in _percents.entries)
        if (e.value > 0) e.key: e.value,
    });
    if (!mounted) return;
    setState(() {
      _loading = false;
      _error = r.error;
      if (r.isOk) _result = r.data;
    });
  }

  void _onChanged(String categoryId, double value) {
    setState(() => _percents[categoryId] = value);
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 350), _calculate);
  }

  @override
  Widget build(BuildContext context) {
    final r = _result;
    final categories = List<Map<String, dynamic>>.from(
      r?['categories'] ?? const [],
    );
    return PageScaffold(
      title: 'Azaltım senaryosu',
      subtitle: 'Son 12 ayın onaylı verisiyle, yıllık',
      body: _loading
          ? const SkeletonList(count: 3)
          : _error != null && r == null
          ? Center(
              child: ErrorState(message: _error!, onRetry: _calculate),
            )
          : categories.isEmpty
          ? const Padding(
              padding: EdgeInsets.all(16),
              child: EmptyState(
                icon: Icons.tune_rounded,
                title: 'Hesaplanacak veri yok',
                message:
                    'Son 12 ayda onaylanmış emisyon kaydı olunca senaryo kurabilirsiniz.',
              ),
            )
          : ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 40),
              children: [
                _SummaryCard(result: r!),
                const SizedBox(height: 16),
                const SectionTitle(
                  'Önlemler',
                  subtitle:
                      'Her kategoride tüketimin ne kadar azalacağını girin',
                ),
                for (final c in categories) _measureCard(c),
                const SizedBox(height: 8),
                const Text(
                  'Emisyon = tüketim × sabit katsayı olduğundan tüketimdeki yüzde azalma emisyona aynen yansır. '
                  'Yüzdeleri bir enerji etüdü, sayaç ölçümü veya tedarikçi teklifine dayandırın.',
                  style: TextStyle(color: AppColors.textMuted, fontSize: 12),
                ),
              ],
            ),
    );
  }

  Widget _measureCard(Map<String, dynamic> c) {
    final id = c['categoryId'] as String;
    final pct = _percents[id] ?? 0;
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: GlassCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    '${Fmt.category(c['categoryName'] ?? '')} · Kapsam ${c['scope'] ?? '-'}',
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                ),
                Text(
                  '%${pct.toStringAsFixed(0)}',
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    fontSize: 18,
                  ),
                ),
              ],
            ),
            Text(
              'Yıllık ${Fmt.integer(c['consumption'] as num?)} ${c['unit']} → ${Fmt.co2(c['emissions'] as num?)}',
              style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
            ),
            Slider(
              value: pct,
              max: 50,
              divisions: 50,
              label: '%${pct.toStringAsFixed(0)}',
              onChanged: (v) => _onChanged(id, v),
            ),
            if (pct > 0)
              Text(
                'Tasarruf: ${Fmt.co2(c['saving'] as num?)} / yıl',
                style: const TextStyle(
                  color: AppColors.primary,
                  fontWeight: FontWeight.w700,
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.result});

  final Map<String, dynamic> result;

  @override
  Widget build(BuildContext context) {
    final meets = result['meetsTarget'] as bool?;
    final saving = (result['totalSaving'] ?? 0) as num;
    return GlassCard(
      gradient: LinearGradient(
        colors: [AppColors.primary.withValues(alpha: 0.18), AppColors.surface],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            saving > 0 ? '−${Fmt.co2(saving)}' : 'Önlem seçilmedi',
            style: const TextStyle(fontSize: 30, fontWeight: FontWeight.w800),
          ),
          Text(
            saving > 0
                ? 'yıllık azalma (${Fmt.percent(result['savingPercent'] as num?)})'
                : 'Aşağıdan yüzde girin',
            style: const TextStyle(color: AppColors.textMuted),
          ),
          const SizedBox(height: 12),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              StatusChip(
                label: 'Şu an: ${Fmt.co2(result['baselineEmissions'] as num?)}',
                color: AppColors.textMuted,
              ),
              StatusChip(
                label:
                    'Senaryo: ${Fmt.co2(result['reducedEmissions'] as num?)}',
                color: AppColors.accent,
              ),
              if (result['targetEmissions'] != null)
                StatusChip(
                  label:
                      '${result['targetYear']} hedefi: ${Fmt.co2(result['targetEmissions'] as num?)}',
                  color: AppColors.primary,
                ),
              if (meets != null)
                StatusChip(
                  label: meets ? 'Hedefi karşılıyor' : 'Hedefe yetmiyor',
                  color: meets ? AppColors.primary : AppColors.warning,
                  icon: meets ? Icons.check_circle_outline : Icons.info_outline,
                ),
            ],
          ),
          if (result['isProjected'] == true) ...[
            const SizedBox(height: 10),
            Text(
              'Son 12 ayda yalnızca ${result['monthsWithData']} ay veri var; tutarlar bu ayların ortalamasıyla yıllığa ölçeklendi.',
              style: const TextStyle(color: AppColors.warning, fontSize: 12),
            ),
          ],
        ],
      ),
    );
  }
}

// ───────────────────────────── Raporlar ─────────────────────────────

/// Yıllık envanter özeti: PDF (sunum/arşiv) ve Excel (kayıt kayıt döküm, katsayı ve kaynağıyla).
class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  int _year = DateTime.now().year;
  String? _busy;

  Future<void> _export(String format) async {
    setState(() => _busy = format);
    final r = await CompanyServices.inventoryReport(
      await _superAdminCompany(),
      _year,
      format,
    );
    if (!mounted) return;
    setState(() => _busy = null);
    if (!r.isOk) {
      showToast(context, r.error!, error: true);
      return;
    }
    final name = 'envanter_$_year.$format';
    final mime = format == 'pdf'
        ? 'application/pdf'
        : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
    await SharePlus.instance.share(
      ShareParams(
        files: [
          XFile.fromData(
            Uint8List.fromList(r.data!),
            name: name,
            mimeType: mime,
          ),
        ],
        fileNameOverrides: [name],
        subject: 'Sera gazı envanteri $_year',
      ),
    );
  }

  Future<void> _createPassport() async {
    final productController = TextEditingController();
    final quantityController = TextEditingController(text: '1');
    final shipmentController = TextEditingController();
    final productCodeController = TextEditingController();
    final formKey = GlobalKey<FormState>();
    final values = await showDialog<Map<String, String>>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Export Passport oluştur'),
        content: Form(
          key: formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: productController,
                  decoration: const InputDecoration(labelText: 'Ürün adı'),
                  validator: (v) => v == null || v.trim().length < 2
                      ? 'Ürün adını girin'
                      : null,
                ),
                TextFormField(
                  controller: productCodeController,
                  decoration: const InputDecoration(
                    labelText: 'Ürün kodu (opsiyonel)',
                  ),
                ),
                TextFormField(
                  controller: shipmentController,
                  decoration: const InputDecoration(
                    labelText: 'Sevkiyat referansı (opsiyonel)',
                  ),
                ),
                TextFormField(
                  controller: quantityController,
                  decoration: const InputDecoration(labelText: 'Miktar (ton)'),
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  validator: (v) =>
                      double.tryParse((v ?? '').replaceAll(',', '.')) == null
                      ? 'Geçerli bir miktar girin'
                      : null,
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Vazgeç'),
          ),
          ElevatedButton(
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.pop(context, {
                  'productName': productController.text.trim(),
                  'productCode': productCodeController.text.trim(),
                  'shipmentReference': shipmentController.text.trim(),
                  'quantity': quantityController.text.replaceAll(',', '.'),
                });
              }
            },
            child: const Text('Oluştur'),
          ),
        ],
      ),
    );
    productController.dispose();
    quantityController.dispose();
    shipmentController.dispose();
    productCodeController.dispose();
    if (values == null || !mounted) return;

    setState(() => _busy = 'passport');
    final companyId = await _superAdminCompany();
    final created = await CompanyServices.createExportPassport(
      companyId: companyId,
      productName: values['productName']!,
      productCode: values['productCode'],
      shipmentReference: values['shipmentReference'],
      quantity: double.parse(values['quantity']!),
      year: _year,
    );
    if (!mounted) return;
    if (!created.isOk) {
      setState(() => _busy = null);
      showToast(context, created.error!, error: true);
      return;
    }
    final passport = created.data!;
    final pdf = await CompanyServices.exportPassportPdf(
      passport['id'].toString(),
      companyId,
    );
    if (!mounted) return;
    setState(() => _busy = null);
    if (!pdf.isOk) {
      showToast(context, pdf.error!, error: true);
      return;
    }
    final relativeUrl = passport['publicUrl']?.toString() ?? '';
    final publicUrl = relativeUrl.startsWith('/api/')
        ? '${ApiClient.baseUrl.substring(0, ApiClient.baseUrl.length - 4)}$relativeUrl'
        : relativeUrl;
    await SharePlus.instance.share(
      ShareParams(
        files: [
          XFile.fromData(
            Uint8List.fromList(pdf.data!),
            name: 'carbontrace-export-passport.pdf',
            mimeType: 'application/pdf',
          ),
        ],
        fileNameOverrides: const ['carbontrace-export-passport.pdf'],
        text: 'CarbonTrace Export Passport\n$publicUrl',
        subject: 'CarbonTrace Export Passport',
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now().year;
    return PageScaffold(
      title: 'Raporlar',
      subtitle: 'Yıllık sera gazı envanter özeti',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 40),
        children: [
          GlassCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                DropdownButtonFormField<int>(
                  initialValue: _year,
                  decoration: const InputDecoration(labelText: 'Rapor yılı'),
                  items: [
                    for (var y = now; y >= now - 4; y--)
                      DropdownMenuItem(value: y, child: Text('$y')),
                  ],
                  onChanged: (v) => setState(() => _year = v ?? now),
                ),
                const SizedBox(height: 16),
                ElevatedButton.icon(
                  onPressed: _busy == null ? () => _export('pdf') : null,
                  icon: _busy == 'pdf'
                      ? const _Spinner()
                      : const Icon(Icons.picture_as_pdf_outlined),
                  label: const Text('PDF özet'),
                ),
                const SizedBox(height: 10),
                OutlinedButton.icon(
                  onPressed: _busy == null ? () => _export('xlsx') : null,
                  icon: _busy == 'xlsx'
                      ? const _Spinner()
                      : const Icon(Icons.table_chart_outlined),
                  label: const Text('Excel (kayıt kayıt döküm)'),
                ),
                const SizedBox(height: 10),
                FilledButton.icon(
                  onPressed: _busy == null ? _createPassport : null,
                  icon: _busy == 'passport'
                      ? const _Spinner()
                      : const Icon(Icons.public_outlined),
                  label: const Text('Export Passport oluştur ve paylaş'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          const GlassCard(
            child: Text(
              'Raporda yalnızca onaylanmış kayıtlar yer alır. Her kalem, hesaplandığı katsayı ve kaynağıyla listelenir; '
              'resmi kaynağa bağlanmamış (örnek) katsayılar ayrıca işaretlenir. '
              'Bu bir kurum içi envanter özetidir, üçüncü taraf doğrulama raporu değildir.',
              style: TextStyle(color: AppColors.textMuted, fontSize: 12.5),
            ),
          ),
        ],
      ),
    );
  }
}

class _Spinner extends StatelessWidget {
  const _Spinner();

  @override
  Widget build(BuildContext context) => const SizedBox(
    width: 18,
    height: 18,
    child: CircularProgressIndicator(strokeWidth: 2),
  );
}
