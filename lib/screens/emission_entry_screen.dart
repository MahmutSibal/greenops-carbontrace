import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../core/formatters.dart';
import '../services/ai_service.dart';
import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../services/emission_service.dart';
import '../services/facility_service.dart';
import '../theme/app_theme.dart';
import '../widgets/aurora_background.dart';
import '../widgets/ui.dart';

class EmissionEntryScreen extends StatefulWidget {
  const EmissionEntryScreen({super.key});

  @override
  State<EmissionEntryScreen> createState() => EmissionEntryScreenState();
}

/// Durum filtresi: null = tümü.
const _statuses = <String?, (String, Color, IconData)>{
  null: ('Tümü', AppColors.textMuted, Icons.all_inclusive_rounded),
  'Pending': ('Beklemede', AppColors.warning, Icons.schedule_rounded),
  'Approved': ('Onaylı', AppColors.primary, Icons.check_circle_outline_rounded),
  'Rejected': ('Reddedildi', AppColors.danger, Icons.cancel_outlined),
};

class EmissionEntryScreenState extends State<EmissionEntryScreen> {
  List<Map<String, dynamic>> _entries = [];
  int _total = 0;
  bool _loading = true;
  String? _error;
  String? _status;
  UserRole _role = UserRole.personnel;

  @override
  void initState() {
    super.initState();
    reload();
  }

  /// Dışarıdan (ör. dashboard'daki "Onay bekliyor") filtreyle açmak için.
  void showPending() {
    setState(() => _status = 'Pending');
    reload();
  }

  Future<void> reload() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final role = await AuthService.getRole();
    final res = await EmissionService.list(pageSize: 100);
    if (!mounted) return;
    setState(() {
      _role = role;
      _loading = false;
      _error = res.error;
      _entries = List<Map<String, dynamic>>.from(res.data?['items'] ?? []);
      _total = (res.data?['totalCount'] ?? 0) as int;
    });
  }

  List<Map<String, dynamic>> get _filtered =>
      _status == null ? _entries : _entries.where((e) => e['status'] == _status).toList();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      floatingActionButton: Padding(
        padding: const EdgeInsets.only(bottom: 84),
        child: FloatingActionButton.extended(
          onPressed: _openCreate,
          icon: const Icon(Icons.add_rounded),
          label: const Text('Yeni kayıt'),
        ),
      ),
      body: AuroraBackground(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Row(
                children: [
                  const Expanded(
                    child: Text('Emisyon kayıtları', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
                  ),
                  if (!_loading) Text('$_total kayıt', style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                ],
              ),
            ),
            SizedBox(
              height: 48,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
                children: [
                  for (final MapEntry(key: key, value: (label, color, icon)) in _statuses.entries)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        selected: _status == key,
                        onSelected: (_) => setState(() => _status = key),
                        avatar: Icon(icon, size: 15, color: _status == key ? color : AppColors.textMuted),
                        label: Text(
                          key == null ? label : '$label · ${_entries.where((e) => e['status'] == key).length}',
                        ),
                      ),
                    ),
                ],
              ),
            ),
            Expanded(child: _buildList()),
          ],
        ),
      ),
    );
  }

  Widget _buildList() {
    if (_loading) return const SkeletonList();
    if (_error != null) return Center(child: ErrorState(message: _error!, onRetry: reload));

    final items = _filtered;
    if (items.isEmpty) {
      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          children: [
            EmptyState(
              icon: Icons.cloud_upload_outlined,
              title: _status == null ? 'Henüz emisyon kaydı yok' : 'Bu durumda kayıt yok',
              message: 'Faturanızın fotoğrafını çekin, AI tüketimi ve dönemi sizin için doldursun.',
              action: FilledButton.icon(
                onPressed: _openCreate,
                icon: const Icon(Icons.document_scanner_outlined),
                label: const Text('İlk kaydı oluştur'),
              ),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: reload,
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 170),
        itemCount: items.length,
        separatorBuilder: (_, _) => const SizedBox(height: 10),
        itemBuilder: (_, i) => _EntryCard(entry: items[i], onTap: () => _openDetail(items[i])),
      ),
    );
  }

  Future<void> _openCreate() async {
    final created = await showAppSheet<bool>(context, title: 'Yeni emisyon kaydı', builder: (_) => const _CreateEntryForm());
    if (created == true) {
      if (mounted) showToast(context, 'Kayıt oluşturuldu, onay bekliyor.');
      reload();
    }
  }

  Future<void> _openDetail(Map<String, dynamic> entry) async {
    final changed = await showAppSheet<bool>(
      context,
      title: 'Kayıt detayı',
      builder: (_) => _EntryDetail(id: entry['id'], canReview: _role.atLeast(UserRole.manager)),
    );
    if (changed == true) reload();
  }
}

class _EntryCard extends StatelessWidget {
  const _EntryCard({required this.entry, required this.onTap});

  final Map<String, dynamic> entry;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final (label, color, icon) = _statuses[entry['status']] ?? _statuses[null]!;
    final category = entry['categoryName']?.toString() ?? '-';
    final period = entry['periodStart'] != null ? Fmt.date(entry['periodStart']) : Fmt.date(entry['createdAt']);
    return GlassCard(
      onTap: onTap,
      padding: const EdgeInsets.all(14),
      child: Row(
        children: [
          IconBadge(icon: _categoryIcon(category), color: AppColors.accent, size: 44),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(entry['facilityName']?.toString() ?? '-', maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                const SizedBox(height: 2),
                Text('${_categoryLabel(category)} · ${Fmt.number(entry['value'] as num?)} ${entry['unit'] ?? ''}',
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                const SizedBox(height: 2),
                Text(period, style: const TextStyle(color: AppColors.textMuted, fontSize: 11)),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(Fmt.co2(entry['calculationResult'] as num?), style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13)),
              const SizedBox(height: 6),
              StatusChip(label: label, color: color, icon: icon),
            ],
          ),
        ],
      ),
    );
  }
}

IconData _categoryIcon(String name) => switch (name) {
  'Electricity' => Icons.bolt_rounded,
  'Natural Gas' => Icons.local_fire_department_outlined,
  'Diesel' => Icons.local_gas_station_outlined,
  _ => Icons.eco_outlined,
};

String _categoryLabel(String name) => switch (name) {
  'Electricity' => 'Elektrik',
  'Natural Gas' => 'Doğalgaz',
  'Diesel' => 'Motorin',
  _ => name,
};

/// Katsayının yılı, kaynağı ve resmi kaynağa dayanıp dayanmadığı.
class _FactorSourceLine extends StatelessWidget {
  const _FactorSourceLine({required this.year, required this.source, required this.verified});

  final int year;
  final String? source;
  final bool verified;

  @override
  Widget build(BuildContext context) {
    final color = verified ? AppColors.primary : AppColors.warning;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(verified ? Icons.verified_outlined : Icons.warning_amber_rounded, size: 16, color: color),
        const SizedBox(width: 6),
        Expanded(
          child: Text(
            verified
                ? 'Resmi katsayı ($year): ${source ?? ''}'
                : 'Örnek katsayı — resmi kaynağa dayanmıyor. ${source ?? ''}',
            style: TextStyle(color: color, fontSize: 11.5, height: 1.35),
          ),
        ),
      ],
    );
  }
}

// ───────────────────────────── Detay ─────────────────────────────

class _EntryDetail extends StatefulWidget {
  const _EntryDetail({required this.id, required this.canReview});

  final String id;
  final bool canReview;

  @override
  State<_EntryDetail> createState() => _EntryDetailState();
}

class _EntryDetailState extends State<_EntryDetail> {
  Map<String, dynamic>? _d;
  String? _error;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    EmissionService.detail(widget.id).then((r) {
      if (mounted) {
        setState(() {
          _d = r.data;
          _error = r.error;
        });
      }
    });
  }

  Future<void> _run(Future<ApiResult<void>> Function() action, String success) async {
    setState(() => _busy = true);
    final r = await action();
    if (!mounted) return;
    setState(() => _busy = false);
    if (r.isOk) {
      showToast(context, success);
      Navigator.pop(context, true);
    } else {
      showToast(context, r.error!, error: true);
    }
  }

  Future<void> _reject() async {
    final ctrl = TextEditingController();
    final reason = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.surface,
        title: const Text('Reddetme nedeni'),
        content: TextField(
          controller: ctrl,
          autofocus: true,
          maxLines: 3,
          maxLength: 1000,
          decoration: const InputDecoration(hintText: 'Örn. fatura dönemi hatalı'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Vazgeç')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger, foregroundColor: Colors.white),
            onPressed: () => Navigator.pop(ctx, ctrl.text.trim()),
            child: const Text('Reddet'),
          ),
        ],
      ),
    );
    if (reason == null || reason.isEmpty) return;
    await _run(() => EmissionService.reject(widget.id, reason), 'Kayıt reddedildi.');
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return Text(_error!, style: const TextStyle(color: AppColors.danger));
    final d = _d;
    if (d == null) {
      return const Column(children: [Skeleton(height: 90), SizedBox(height: 12), Skeleton(height: 160)]);
    }

    final (label, color, icon) = _statuses[d['status']] ?? _statuses[null]!;
    final pending = d['status'] == 'Pending';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        GlassCard(
          gradient: LinearGradient(colors: [color.withValues(alpha: 0.15), AppColors.surface]),
          borderColor: color.withValues(alpha: 0.4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              StatusChip(label: label, color: color, icon: icon),
              const SizedBox(height: 10),
              Text(Fmt.co2(d['calculationResult'] as num?), style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800)),
              const SizedBox(height: 4),
              Text(
                '${Fmt.number(d['value'] as num?)} ${d['categoryUnit'] ?? ''} × ${Fmt.factor(d['factorValue'] as num?)} kgCO₂e/${d['categoryUnit'] ?? 'birim'}',
                style: const TextStyle(color: AppColors.textMuted, fontSize: 12.5),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _row('Tesis', d['facilityName']),
        _row('Kategori', _categoryLabel(d['categoryName']?.toString() ?? '-')),
        _row(
          'Dönem',
          d['periodStart'] == null
              ? 'Belirtilmedi'
              : d['periodEnd'] == null
              ? '${Fmt.date(d['periodStart'])} (başlangıç)'
              : '${Fmt.date(d['periodStart'])} – ${Fmt.date(d['periodEnd'])}',
        ),
        _row('Giriş', Fmt.dateTime(d['createdAt'])),
        if (d['approvedAt'] != null) _row('Onay', Fmt.dateTime(d['approvedAt'])),
        if (d['rejectionReason'] != null) _row('Red nedeni', d['rejectionReason']),
        if (d['evidenceUrl'] != null) _row('Kanıt', d['evidenceUrl']),
        if (d['factorYear'] != null) ...[
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.background.withValues(alpha: 0.5),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.border),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Katsayı kaynağı', style: TextStyle(color: AppColors.textMuted, fontSize: 12)),
                const SizedBox(height: 6),
                _FactorSourceLine(
                  year: d['factorYear'] as int,
                  source: d['factorSource'] as String?,
                  verified: d['factorVerified'] == true,
                ),
                if (d['factorSourceUrl'] != null) ...[
                  const SizedBox(height: 6),
                  SelectableText(d['factorSourceUrl'], style: const TextStyle(color: AppColors.accent, fontSize: 10.5)),
                ],
              ],
            ),
          ),
        ],
        const SizedBox(height: 16),
        if (pending && widget.canReview)
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: _busy ? null : _reject,
                  style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger),
                  icon: const Icon(Icons.close_rounded),
                  label: const Text('Reddet'),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: FilledButton.icon(
                  onPressed: _busy ? null : () => _run(() => EmissionService.approve(widget.id), 'Kayıt onaylandı.'),
                  icon: const Icon(Icons.check_rounded),
                  label: const Text('Onayla'),
                ),
              ),
            ],
          ),
        if (pending) ...[
          const SizedBox(height: 8),
          TextButton.icon(
            onPressed: _busy
                ? null
                : () async {
                    if (await confirmDialog(context,
                        title: 'Kaydı sil', message: 'Bu bekleyen kayıt silinecek.', confirmLabel: 'Sil', destructive: true)) {
                      await _run(() => EmissionService.delete(widget.id), 'Kayıt silindi.');
                    }
                  },
            icon: const Icon(Icons.delete_outline_rounded, color: AppColors.danger),
            label: const Text('Kaydı sil', style: TextStyle(color: AppColors.danger)),
          ),
        ],
      ],
    );
  }

  Widget _row(String label, dynamic value) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 7),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(width: 92, child: Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 13))),
        Expanded(child: Text(value?.toString() ?? '-', style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600))),
      ],
    ),
  );
}

// ───────────────────────────── Yeni kayıt + AI fatura okuma ─────────────────────────────

class _CreateEntryForm extends StatefulWidget {
  const _CreateEntryForm();

  @override
  State<_CreateEntryForm> createState() => _CreateEntryFormState();
}

class _CreateEntryFormState extends State<_CreateEntryForm> {
  final _valueCtrl = TextEditingController();
  List<Map<String, dynamic>> _facilities = [];
  List<Map<String, dynamic>> _categories = [];
  List<Map<String, dynamic>> _factors = [];

  /// Backend'deki kuralın aynısı: aktif ve dönem yılından küçük/eşit en güncel katsayı.
  Map<String, dynamic>? get _applicableFactor {
    if (_categoryId == null) return null;
    final candidates = _factors
        .where((f) => f['categoryId'] == _categoryId && (f['year'] as int) <= _period.year)
        .toList()
      ..sort((a, b) => (b['year'] as int).compareTo(a['year'] as int));
    return candidates.firstOrNull;
  }
  String? _facilityId;
  String? _categoryId;
  late DateTime _period = DateTime(DateTime.now().year, DateTime.now().month - 1);
  bool _loading = true;
  bool _saving = false;
  bool _scanning = false;
  Map<String, dynamic>? _scan;
  String? _error;

  @override
  void initState() {
    super.initState();
    _valueCtrl.addListener(() => setState(() {}));
    _load();
  }

  @override
  void dispose() {
    _valueCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final results = await Future.wait([
      FacilityService.getAll(),
      CategoryService.getAll(),
      ApiClient.get('/factors'),
    ]);
    if (!mounted) return;
    final factorRes = results[2] as dynamic;
    var factors = <Map<String, dynamic>>[];
    if (ApiClient.isOk(factorRes)) {
      factors = (jsonDecode(factorRes.body) as List).cast<Map<String, dynamic>>()
          .where((f) => f['isActive'] == true)
          .toList();
    }
    setState(() {
      _facilities = results[0] as List<Map<String, dynamic>>;
      _categories = results[1] as List<Map<String, dynamic>>;
      _factors = factors;
      if (_facilities.length == 1) _facilityId = _facilities.first['id'];
      _loading = false;
    });
  }

  Future<void> _scanInvoice(ImageSource source) async {
    final picked = await ImagePicker().pickImage(source: source, maxWidth: 2000, imageQuality: 85);
    if (picked == null) return;
    setState(() {
      _scanning = true;
      _error = null;
    });
    final res = await AiService.scanInvoice(await picked.readAsBytes(), picked.name);
    if (!mounted) return;
    setState(() {
      _scanning = false;
      _scan = res.data;
      _error = res.error;
    });
    final d = res.data;
    if (d == null) return;

    // AI sonucu formu sadece önerir; kullanıcı kontrol edip kaydeder.
    setState(() {
      if (d['consumption'] != null) _valueCtrl.text = (d['consumption'] as num).toString();
      if (d['suggestedCategoryId'] != null) _categoryId = d['suggestedCategoryId'];
      final start = Fmt.parse(d['periodStart']);
      if (start != null) _period = DateTime(start.year, start.month);
    });
  }

  Future<void> _pickMonth() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _period,
      firstDate: DateTime(now.year - 5),
      lastDate: now,
      helpText: 'Faaliyet dönemi (ayın herhangi bir günü)',
      locale: const Locale('tr', 'TR'),
    );
    if (picked != null) setState(() => _period = DateTime(picked.year, picked.month));
  }

  double? get _value => double.tryParse(_valueCtrl.text.replaceAll(',', '.'));

  Map<String, dynamic>? get _category =>
      _categories.where((c) => c['id'] == _categoryId).firstOrNull;

  Future<void> _save() async {
    final value = _value;
    if (_facilityId == null || _categoryId == null || value == null || value <= 0) {
      setState(() => _error = 'Tesis, kategori ve 0\'dan büyük bir değer girin.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    final res = await EmissionService.create(
      facilityId: _facilityId!,
      categoryId: _categoryId!,
      value: value,
      periodStart: _period,
      periodEnd: DateTime(_period.year, _period.month + 1, 0),
    );
    if (!mounted) return;
    if (res.isOk) {
      Navigator.pop(context, true);
    } else {
      setState(() {
        _saving = false;
        _error = res.error;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Column(children: [Skeleton(height: 90), SizedBox(height: 12), Skeleton(height: 200)]);

    if (_facilities.isEmpty) {
      return const EmptyState(
        icon: Icons.apartment_rounded,
        title: 'Önce bir tesis ekleyin',
        message: 'Emisyon kayıtları bir tesise bağlanır. Tesisler sekmesinden ekleyebilirsiniz.',
      );
    }

    final unit = _category?['unit']?.toString() ?? '';
    final applicable = _applicableFactor;
    final factor = (applicable?['factor'] as num?)?.toDouble();
    final preview = (factor != null && _value != null) ? _value! * factor : null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _buildScanCard(),
        const SizedBox(height: 16),
        DropdownButtonFormField<String>(
          initialValue: _facilityId,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'Tesis', prefixIcon: Icon(Icons.apartment_rounded)),
          items: [for (final f in _facilities) DropdownMenuItem(value: f['id'] as String, child: Text(f['name']))],
          onChanged: (v) => setState(() => _facilityId = v),
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final c in _categories)
              ChoiceChip(
                selected: _categoryId == c['id'],
                onSelected: (_) => setState(() => _categoryId = c['id']),
                avatar: Icon(_categoryIcon(c['name']), size: 16,
                    color: _categoryId == c['id'] ? AppColors.primary : AppColors.textMuted),
                label: Text('${_categoryLabel(c['name'])} (${c['unit']})'),
              ),
          ],
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _valueCtrl,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: InputDecoration(
            labelText: 'Tüketim miktarı',
            prefixIcon: const Icon(Icons.speed_rounded),
            suffixText: unit,
          ),
        ),
        const SizedBox(height: 12),
        InkWell(
          onTap: _pickMonth,
          borderRadius: BorderRadius.circular(14),
          child: InputDecorator(
            decoration: const InputDecoration(labelText: 'Faaliyet dönemi', prefixIcon: Icon(Icons.calendar_month_rounded)),
            child: Text(Fmt.monthYear(_period)),
          ),
        ),
        if (preview != null) ...[
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Icon(Icons.calculate_outlined, color: AppColors.primary, size: 20),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text('Tahmini sonuç: ${Fmt.co2(preview)}',
                          style: const TextStyle(fontWeight: FontWeight.w700)),
                    ),
                    Text('× ${Fmt.factor(factor)}', style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                  ],
                ),
                const SizedBox(height: 8),
                _FactorSourceLine(
                  year: applicable!['year'] as int,
                  source: applicable['source'] as String?,
                  verified: applicable['isVerified'] == true,
                ),
              ],
            ),
          ),
        ],
        if (_error != null) ...[
          const SizedBox(height: 12),
          Text(_error!, style: const TextStyle(color: AppColors.danger, fontSize: 13)),
        ],
        const SizedBox(height: 18),
        ElevatedButton(
          onPressed: _saving || _scanning ? null : _save,
          child: _saving
              ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFF052414)))
              : const Text('Kaydet ve onaya gönder'),
        ),
      ],
    );
  }

  Widget _buildScanCard() {
    final scan = _scan;
    final warnings = (scan?['warnings'] as List?)?.cast<String>() ?? const [];
    final confidence = ((scan?['confidence'] ?? 0) as num).toDouble();

    return GlassCard(
      padding: const EdgeInsets.all(14),
      borderColor: const Color(0xFFB18CFF).withValues(alpha: 0.4),
      gradient: LinearGradient(colors: [const Color(0xFFB18CFF).withValues(alpha: 0.14), AppColors.surface]),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const IconBadge(icon: Icons.document_scanner_outlined, color: Color(0xFFB18CFF), size: 38),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Faturadan AI ile doldur', style: TextStyle(fontWeight: FontWeight.w800)),
                    Text(
                      _scanning ? 'qwen3-vl okuyor, gemma4 yorumluyor… (30-60 sn)' : 'Elektrik, doğalgaz veya yakıt faturası',
                      style: const TextStyle(color: AppColors.textMuted, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (_scanning)
            const LinearProgressIndicator(color: Color(0xFFB18CFF), backgroundColor: AppColors.border)
          else
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => _scanInvoice(ImageSource.camera),
                    icon: const Icon(Icons.photo_camera_outlined, size: 18),
                    label: const Text('Kamera'),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => _scanInvoice(ImageSource.gallery),
                    icon: const Icon(Icons.photo_library_outlined, size: 18),
                    label: const Text('Galeri'),
                  ),
                ),
              ],
            ),
          if (scan != null) ...[
            const SizedBox(height: 12),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                StatusChip(
                  label: 'Güven %${(confidence * 100).round()}',
                  color: confidence >= 0.8 ? AppColors.primary : confidence >= 0.5 ? AppColors.warning : AppColors.danger,
                  icon: Icons.insights_rounded,
                ),
                if (scan['provider'] != null) StatusChip(label: scan['provider'], color: AppColors.accent),
                if (scan['consumption'] != null)
                  StatusChip(label: '${Fmt.number(scan['consumption'] as num)} ${scan['unit'] ?? ''}', color: AppColors.accent),
              ],
            ),
            for (final w in warnings)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Row(
                  children: [
                    const Icon(Icons.warning_amber_rounded, color: AppColors.warning, size: 16),
                    const SizedBox(width: 6),
                    Expanded(child: Text(w, style: const TextStyle(color: AppColors.warning, fontSize: 12))),
                  ],
                ),
              ),
            const SizedBox(height: 8),
            const Text('Alanlar dolduruldu — kaydetmeden önce kontrol edin.',
                style: TextStyle(color: AppColors.textMuted, fontSize: 11.5)),
          ],
        ],
      ),
    );
  }
}
