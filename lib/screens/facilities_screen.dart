import 'package:flutter/material.dart';

import '../core/formatters.dart';
import '../services/auth_service.dart';
import '../services/dashboard_service.dart';
import '../services/facility_service.dart';
import '../theme/app_theme.dart';
import '../widgets/aurora_background.dart';
import '../widgets/ui.dart';

class FacilitiesScreen extends StatefulWidget {
  const FacilitiesScreen({super.key});

  @override
  State<FacilitiesScreen> createState() => _FacilitiesScreenState();
}

class _FacilitiesScreenState extends State<FacilitiesScreen> {
  List<Map<String, dynamic>> _facilities = [];
  Map<String, num> _emissions = {};
  bool _loading = true;
  String? _error;
  String? _companyId;
  UserRole _role = UserRole.personnel;

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
    final role = await AuthService.getRole();
    final cid = await AuthService.getCompanyId();
    if (cid == null) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = 'Şirket bilgisi bulunamadı.';
      });
      return;
    }
    final res = await FacilityService.list(companyId: cid);
    final comparison = await DashboardService.getFacilityComparison(companyId: cid);
    if (!mounted) return;
    setState(() {
      _role = role;
      _companyId = cid;
      _facilities = res.data ?? [];
      _error = res.error;
      _emissions = {for (final c in comparison) c['facilityId'] as String: (c['totalEmissions'] ?? 0) as num};
      _loading = false;
    });
  }

  bool get _canManage => _role.atLeast(UserRole.manager);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      floatingActionButton: _canManage
          ? Padding(
              padding: const EdgeInsets.only(bottom: 84),
              child: FloatingActionButton.extended(
                onPressed: _openCreate,
                icon: const Icon(Icons.add_business_outlined),
                label: const Text('Tesis ekle'),
              ),
            )
          : null,
      body: AuroraBackground(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
              child: Row(
                children: [
                  const Expanded(child: Text('Tesisler', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800))),
                  if (!_loading) Text('${_facilities.length} aktif', style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                ],
              ),
            ),
            Expanded(child: _buildBody()),
          ],
        ),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const SkeletonList(itemHeight: 110);
    if (_error != null) return Center(child: ErrorState(message: _error!, onRetry: _load));
    if (_facilities.isEmpty) {
      return EmptyState(
        icon: Icons.apartment_rounded,
        title: 'Henüz tesis yok',
        message: 'Emisyonlarınızı izlemek için fabrika, ofis veya depolarınızı ekleyin.',
        action: _canManage
            ? FilledButton.icon(onPressed: _openCreate, icon: const Icon(Icons.add_rounded), label: const Text('İlk tesisi ekle'))
            : null,
      );
    }

    final maxEmission = _emissions.values.fold<num>(0, (a, b) => a > b ? a : b);
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 170),
        itemCount: _facilities.length,
        separatorBuilder: (_, _) => const SizedBox(height: 10),
        itemBuilder: (_, i) {
          final f = _facilities[i];
          final emission = _emissions[f['id']] ?? 0;
          final employees = f['employeeCount'] as int?;
          return GlassCard(
            onTap: _role.atLeast(UserRole.companyAdmin) ? () => _confirmDelete(f) : null,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const IconBadge(icon: Icons.factory_outlined, color: AppColors.accent, size: 44),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(f['name'] ?? '-', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 15)),
                          if ((f['address'] ?? '').toString().isNotEmpty)
                            Text(f['address'], maxLines: 1, overflow: TextOverflow.ellipsis,
                                style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                        ],
                      ),
                    ),
                    if (f['department'] != null) StatusChip(label: f['department'], color: AppColors.textMuted),
                  ],
                ),
                const SizedBox(height: 14),
                Row(
                  children: [
                    _metric(Icons.cloud_outlined, Fmt.co2(emission), 'Onaylı emisyon'),
                    _metric(Icons.groups_outlined, employees == null ? '-' : Fmt.integer(employees), 'Çalışan'),
                    _metric(
                      Icons.person_outline,
                      employees == null || employees == 0 ? '-' : Fmt.co2Axis(emission / employees),
                      'kg / kişi',
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                ClipRRect(
                  borderRadius: BorderRadius.circular(6),
                  child: LinearProgressIndicator(
                    value: maxEmission == 0 ? 0 : emission / maxEmission,
                    minHeight: 6,
                    backgroundColor: AppColors.border.withValues(alpha: 0.6),
                    valueColor: const AlwaysStoppedAnimation(AppColors.primary),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _metric(IconData icon, String value, String label) => Expanded(
    child: Row(
      children: [
        Icon(icon, size: 16, color: AppColors.textMuted),
        const SizedBox(width: 6),
        Flexible(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(value, maxLines: 1, overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 12.5)),
              Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 10)),
            ],
          ),
        ),
      ],
    ),
  );

  Future<void> _confirmDelete(Map<String, dynamic> f) async {
    final ok = await confirmDialog(
      context,
      title: '${f['name']} pasife alınsın mı?',
      message: 'Tesis listeden kaldırılır; geçmiş emisyon kayıtları raporlarda korunur.',
      confirmLabel: 'Pasife al',
      destructive: true,
    );
    if (!ok) return;
    final res = await FacilityService.delete(f['id']);
    if (!mounted) return;
    showToast(context, res.isOk ? 'Tesis pasife alındı.' : res.error!, error: !res.isOk);
    if (res.isOk) _load();
  }

  Future<void> _openCreate() async {
    final created = await showAppSheet<bool>(
      context,
      title: 'Yeni tesis',
      builder: (_) => _FacilityForm(companyId: _companyId!),
    );
    if (created == true) {
      if (mounted) showToast(context, 'Tesis eklendi.');
      _load();
    }
  }
}

class _FacilityForm extends StatefulWidget {
  const _FacilityForm({required this.companyId});

  final String companyId;

  @override
  State<_FacilityForm> createState() => _FacilityFormState();
}

class _FacilityFormState extends State<_FacilityForm> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _address = TextEditingController();
  final _department = TextEditingController();
  final _employees = TextEditingController();
  final _area = TextEditingController();
  bool _saving = false;
  String? _error;

  @override
  void dispose() {
    for (final c in [_name, _address, _department, _employees, _area]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _saving = true;
      _error = null;
    });
    final res = await FacilityService.create(
      companyId: widget.companyId,
      name: _name.text.trim(),
      address: _address.text.trim(),
      department: _department.text.trim(),
      employeeCount: int.tryParse(_employees.text),
      squareMeters: double.tryParse(_area.text.replaceAll(',', '.')),
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
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: _name,
            textCapitalization: TextCapitalization.words,
            decoration: const InputDecoration(labelText: 'Tesis adı', prefixIcon: Icon(Icons.factory_outlined)),
            validator: (v) => (v == null || v.trim().isEmpty) ? 'Tesis adı gerekli' : null,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _address,
            decoration: const InputDecoration(labelText: 'Adres', prefixIcon: Icon(Icons.place_outlined)),
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _department,
            decoration: const InputDecoration(labelText: 'Departman (isteğe bağlı)', prefixIcon: Icon(Icons.account_tree_outlined)),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: TextFormField(
                  controller: _employees,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Çalışan', prefixIcon: Icon(Icons.groups_outlined)),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: TextFormField(
                  controller: _area,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(labelText: 'Alan', suffixText: 'm²', prefixIcon: Icon(Icons.square_foot_rounded)),
                ),
              ),
            ],
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: const TextStyle(color: AppColors.danger, fontSize: 13)),
          ],
          const SizedBox(height: 18),
          ElevatedButton(
            onPressed: _saving ? null : _save,
            child: _saving
                ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFF052414)))
                : const Text('Tesisi kaydet'),
          ),
        ],
      ),
    );
  }
}
