import 'package:flutter/material.dart';

import '../core/formatters.dart';
import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../services/company_services.dart';
import '../theme/app_theme.dart';
import '../widgets/ui.dart';

/// Liste tabanlı modül sayfaları için yükleme/hata/boş durumlarını yöneten iskelet.
class _AsyncList extends StatelessWidget {
  const _AsyncList({
    required this.loading,
    required this.error,
    required this.isEmpty,
    required this.onRefresh,
    required this.empty,
    required this.children,
  });

  final bool loading;
  final String? error;
  final bool isEmpty;
  final Future<void> Function() onRefresh;
  final Widget empty;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const SkeletonList();
    }
    if (error != null) {
      return Center(
        child: ErrorState(message: error!, onRetry: onRefresh),
      );
    }
    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
        children: isEmpty ? [empty] : children,
      ),
    );
  }
}

Future<String?> _superAdminCompany() async =>
    await AuthService.getRole() == UserRole.superAdmin
    ? AuthService.getCompanyId()
    : null;

// ───────────────────────────── Hedefler ─────────────────────────────

class TargetsScreen extends StatefulWidget {
  const TargetsScreen({super.key});

  @override
  State<TargetsScreen> createState() => _TargetsScreenState();
}

class _TargetsScreenState extends State<TargetsScreen> {
  List<Map<String, dynamic>> _targets = [];
  Map<String, dynamic>? _progress;
  bool _loading = true;
  String? _error;
  bool _canCreate = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final cid = await _superAdminCompany();
    final role = await AuthService.getRole();
    final t = await CompanyServices.targets(cid);
    final p = await CompanyServices.targetProgress(cid);
    if (!mounted) return;
    setState(() {
      _loading = false;
      _error = t.error;
      _targets = t.data ?? [];
      _progress = p.data;
      _canCreate = role.atLeast(UserRole.companyAdmin);
    });
  }

  @override
  Widget build(BuildContext context) {
    final p = _progress;
    final pct = ((p?['progressPercent'] ?? 0) as num).toDouble();
    return PageScaffold(
      title: 'Hedefler',
      subtitle: 'Yıllık (tahmini) emisyon baz yıla göre',
      floatingActionButton: _canCreate
          ? FloatingActionButton.extended(
              onPressed: _create,
              icon: const Icon(Icons.add_rounded),
              label: const Text('Hedef ekle'),
            )
          : null,
      body: _AsyncList(
        loading: _loading,
        error: _error,
        isEmpty: _targets.isEmpty,
        onRefresh: _load,
        empty: EmptyState(
          icon: Icons.flag_outlined,
          title: 'Henüz hedef yok',
          message:
              'Örn. 2030\'a kadar 2024 baz yılına göre %42 azaltım (SBTi uyumlu).',
          action: _canCreate
              ? FilledButton(
                  onPressed: _create,
                  child: const Text('Hedef belirle'),
                )
              : null,
        ),
        children: [
          if (p != null)
            GlassCard(
              gradient: LinearGradient(
                colors: [
                  AppColors.primary.withValues(alpha: 0.18),
                  AppColors.surface,
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(
                        '%${pct.toStringAsFixed(0)}',
                        style: const TextStyle(
                          fontSize: 34,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(width: 10),
                      const Expanded(
                        child: Text(
                          'hedefe ilerleme',
                          style: TextStyle(color: AppColors.textMuted),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: LinearProgressIndicator(
                      value: pct / 100,
                      minHeight: 10,
                      backgroundColor: AppColors.border,
                    ),
                  ),
                  const SizedBox(height: 14),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      StatusChip(
                        label:
                            'Baz: ${Fmt.co2(p['baselineEmissions'] as num?)}',
                        color: AppColors.textMuted,
                      ),
                      StatusChip(
                        label:
                            'Bu yıl (gerçekleşen): ${Fmt.co2(p['currentEmissions'] as num?)}',
                        color: AppColors.accent,
                      ),
                      if (p['isProjected'] == true)
                        StatusChip(
                          label:
                              'Yıllık tahmin: ${Fmt.co2(p['projectedAnnualEmissions'] as num?)} · ${p['monthsCovered']} ay',
                          color: AppColors.warning,
                        ),
                      StatusChip(
                        label:
                            'Hedef: ${Fmt.co2(p['targetEmissions'] as num?)}',
                        color: AppColors.primary,
                      ),
                      if (((p['carbonCreditsOffset'] ?? 0) as num) > 0)
                        StatusChip(
                          label:
                              'Kredi: ${Fmt.co2(p['carbonCreditsOffset'] as num?)}',
                          color: const Color(0xFFB18CFF),
                        ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  OutlinedButton.icon(
                    onPressed: () => Navigator.pushNamed(context, '/scenario'),
                    icon: const Icon(Icons.tune_rounded),
                    label: const Text('Azaltım senaryosu kur'),
                  ),
                ],
              ),
            ),
          const SizedBox(height: 16),
          const SectionTitle('Tanımlı hedefler'),
          for (final t in _targets)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: GlassCard(
                child: Row(
                  children: [
                    const IconBadge(
                      icon: Icons.flag_rounded,
                      color: AppColors.primary,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            '${t['targetYear']}: %${Fmt.number(t['reductionPercent'] as num?)} azaltım',
                            style: const TextStyle(fontWeight: FontWeight.w800),
                          ),
                          Text(
                            'Baz ${t['baselineYear']} · ${Fmt.co2(t['baselineEmissions'] as num?)}',
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 12,
                            ),
                          ),
                          if ((t['description'] ?? '').toString().isNotEmpty)
                            Text(
                              t['description'],
                              style: const TextStyle(fontSize: 12),
                            ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _create() async {
    final year = DateTime.now().year;
    final targetYear = TextEditingController(text: '${year + 4}');
    final baseYear = TextEditingController(text: '${year - 1}');
    final baseline = TextEditingController();
    final percent = TextEditingController(text: '42');
    final desc = TextEditingController();
    final ok = await showAppSheet<bool>(
      context,
      title: 'Yeni azaltım hedefi',
      builder: (ctx) {
        String? error;
        return StatefulBuilder(
          builder: (ctx, setLocal) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: baseYear,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(labelText: 'Baz yıl'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: TextField(
                      controller: targetYear,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(labelText: 'Hedef yıl'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              TextField(
                controller: baseline,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                decoration: const InputDecoration(
                  labelText: 'Baz yıl emisyonu',
                  suffixText: 'kgCO₂e',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: percent,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Azaltım',
                  suffixText: '%',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: desc,
                decoration: const InputDecoration(
                  labelText: 'Açıklama (isteğe bağlı)',
                ),
              ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 10),
                  child: Text(
                    error!,
                    style: const TextStyle(color: AppColors.danger),
                  ),
                ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () async {
                  final r = await CompanyServices.createTarget(
                    companyId: await _superAdminCompany(),
                    targetYear: int.tryParse(targetYear.text) ?? 0,
                    baselineYear: int.tryParse(baseYear.text) ?? 0,
                    baselineEmissions:
                        double.tryParse(baseline.text.replaceAll(',', '.')) ??
                        0,
                    reductionPercent:
                        double.tryParse(percent.text.replaceAll(',', '.')) ?? 0,
                    description: desc.text.trim(),
                  );
                  if (!ctx.mounted) return;
                  r.isOk
                      ? Navigator.pop(ctx, true)
                      : setLocal(() => error = r.error);
                },
                child: const Text('Hedefi kaydet'),
              ),
            ],
          ),
        );
      },
    );
    if (ok == true) {
      if (mounted) showToast(context, 'Hedef oluşturuldu.');
      _load();
    }
  }
}

// ───────────────────────────── Kullanıcılar ─────────────────────────────

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key});

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  List<Map<String, dynamic>> _users = [];
  bool _loading = true;
  String? _error;
  UserRole _role = UserRole.personnel;
  String _myEmail = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final r = await CompanyServices.users(await _superAdminCompany());
    final role = await AuthService.getRole();
    final email = await AuthService.getUserEmail() ?? '';
    if (!mounted) return;
    setState(() {
      _loading = false;
      _error = r.error;
      _users = r.data ?? [];
      _role = role;
      _myEmail = email;
    });
  }

  bool get _isAdmin => _role.atLeast(UserRole.companyAdmin);

  @override
  Widget build(BuildContext context) {
    return PageScaffold(
      title: 'Kullanıcılar',
      subtitle: '${_users.length} kişi',
      floatingActionButton: _isAdmin
          ? FloatingActionButton.extended(
              onPressed: _invite,
              icon: const Icon(Icons.person_add_alt_1_rounded),
              label: const Text('Davet et'),
            )
          : null,
      body: _AsyncList(
        loading: _loading,
        error: _error,
        isEmpty: _users.isEmpty,
        onRefresh: _load,
        empty: const EmptyState(
          icon: Icons.group_outlined,
          title: 'Kullanıcı yok',
        ),
        children: [
          for (final u in _users)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: GlassCard(
                onTap: _isAdmin && u['email'] != _myEmail
                    ? () => _manage(u)
                    : null,
                padding: const EdgeInsets.all(14),
                child: Row(
                  children: [
                    CircleAvatar(
                      backgroundColor:
                          (u['isActive'] == true
                                  ? AppColors.primary
                                  : AppColors.textMuted)
                              .withValues(alpha: 0.18),
                      child: Text(
                        (u['fullName'] ?? '?')
                            .toString()
                            .substring(0, 1)
                            .toUpperCase(),
                        style: TextStyle(
                          color: u['isActive'] == true
                              ? AppColors.primary
                              : AppColors.textMuted,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            u['fullName'] ?? '-',
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          Text(
                            u['email'] ?? '',
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 12,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        StatusChip(
                          label: UserRoleX.parse(u['role']).label,
                          color: AppColors.accent,
                        ),
                        if (u['isActive'] != true) ...[
                          const SizedBox(height: 4),
                          const StatusChip(
                            label: 'Pasif',
                            color: AppColors.danger,
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _manage(Map<String, dynamic> u) async {
    final active = u['isActive'] == true;
    final changed = await showAppSheet<bool>(
      context,
      title: u['fullName'] ?? 'Kullanıcı',
      builder: (ctx) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Rol',
            style: TextStyle(color: AppColors.textMuted, fontSize: 12),
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: [
              for (final r in [
                'Manager',
                'Personnel',
                if (_role == UserRole.superAdmin) 'CompanyAdmin',
              ])
                ChoiceChip(
                  selected: u['role'] == r,
                  label: Text(UserRoleX.parse(r).label),
                  onSelected: (_) async {
                    final res = await CompanyServices.setUserRole(u['id'], r);
                    if (!ctx.mounted) return;
                    showToast(
                      ctx,
                      res.isOk ? 'Rol güncellendi.' : res.error!,
                      error: !res.isOk,
                    );
                    if (res.isOk) Navigator.pop(ctx, true);
                  },
                ),
            ],
          ),
          const SizedBox(height: 18),
          OutlinedButton.icon(
            style: OutlinedButton.styleFrom(
              foregroundColor: active ? AppColors.danger : AppColors.primary,
            ),
            onPressed: () async {
              final res = await CompanyServices.setUserStatus(u['id'], !active);
              if (!ctx.mounted) return;
              showToast(
                ctx,
                res.isOk
                    ? (active ? 'Hesap donduruldu.' : 'Hesap aktif edildi.')
                    : res.error!,
                error: !res.isOk,
              );
              if (res.isOk) Navigator.pop(ctx, true);
            },
            icon: Icon(
              active ? Icons.block_rounded : Icons.check_circle_outline_rounded,
            ),
            label: Text(active ? 'Hesabı dondur' : 'Hesabı aktif et'),
          ),
        ],
      ),
    );
    if (changed == true) _load();
  }

  Future<void> _invite() async {
    final name = TextEditingController();
    final email = TextEditingController();
    final password = TextEditingController();
    String role = 'Personnel';
    final ok = await showAppSheet<Map<String, dynamic>>(
      context,
      title: 'Ekip üyesi ekle',
      builder: (ctx) {
        String? error;
        return StatefulBuilder(
          builder: (ctx, setLocal) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextField(
                controller: name,
                decoration: const InputDecoration(
                  labelText: 'Ad Soyad',
                  prefixIcon: Icon(Icons.person_outline),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: email,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(
                  labelText: 'E-posta',
                  prefixIcon: Icon(Icons.mail_outline),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: password,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Geçici şifre',
                  helperText: 'En az 8 karakter, harf ve rakam',
                  prefixIcon: Icon(Icons.lock_outline),
                ),
              ),
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                children: [
                  for (final r in [
                    'Personnel',
                    'Manager',
                    if (_role == UserRole.superAdmin) 'CompanyAdmin',
                  ])
                    ChoiceChip(
                      selected: role == r,
                      label: Text(UserRoleX.parse(r).label),
                      onSelected: (_) => setLocal(() => role = r),
                    ),
                ],
              ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 10),
                  child: Text(
                    error!,
                    style: const TextStyle(color: AppColors.danger),
                  ),
                ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () async {
                  final r = await CompanyServices.inviteUser(
                    companyId: await _superAdminCompany(),
                    fullName: name.text.trim(),
                    email: email.text.trim(),
                    role: role,
                    password: password.text,
                  );
                  if (!ctx.mounted) return;
                  r.isOk
                      ? Navigator.pop(ctx, r.data)
                      : setLocal(() => error = r.error);
                },
                child: const Text('Kullanıcıyı oluştur'),
              ),
            ],
          ),
        );
      },
    );
    if (ok is Map<String, dynamic>) {
      final temporaryPassword = ok['temporaryPassword'] as String?;
      if (mounted) {
        showToast(
          context,
          temporaryPassword == null
              ? 'Kullanıcı eklendi.'
              : 'Kullanıcı eklendi. Geçici şifre: $temporaryPassword',
        );
      }
      _load();
    }
  }
}

// ───────────────────────────── Tedarikçiler ─────────────────────────────

class SuppliersScreen extends StatefulWidget {
  const SuppliersScreen({super.key});

  @override
  State<SuppliersScreen> createState() => _SuppliersScreenState();
}

class _SuppliersScreenState extends State<SuppliersScreen> {
  List<Map<String, dynamic>> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final r = await CompanyServices.suppliers(await _superAdminCompany());
    if (!mounted) return;
    setState(() {
      _loading = false;
      _error = r.error;
      _items = r.data ?? [];
    });
  }

  Color _esgColor(int s) => s >= 70
      ? AppColors.primary
      : s >= 40
      ? AppColors.warning
      : AppColors.danger;

  @override
  Widget build(BuildContext context) {
    return PageScaffold(
      title: 'Tedarikçiler',
      subtitle: 'Kapsam 3 · değer zinciri',
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _add,
        icon: const Icon(Icons.add_rounded),
        label: const Text('Tedarikçi'),
      ),
      body: _AsyncList(
        loading: _loading,
        error: _error,
        isEmpty: _items.isEmpty,
        onRefresh: _load,
        empty: const EmptyState(
          icon: Icons.local_shipping_outlined,
          title: 'Tedarikçi eklenmedi',
          message:
              'Kapsam 3 emisyonlarının büyük kısmı tedarik zincirinden gelir.',
        ),
        children: [
          for (final s in _items)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: GlassCard(
                onTap: () => _score(s),
                padding: const EdgeInsets.all(14),
                child: Row(
                  children: [
                    const IconBadge(
                      icon: Icons.local_shipping_outlined,
                      color: AppColors.accent,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            s['name'] ?? '-',
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          Text(
                            s['contactEmail'] ?? '',
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 12,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Column(
                      children: [
                        Text(
                          '${s['esgScore'] ?? 0}',
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                            color: _esgColor(s['esgScore'] ?? 0),
                          ),
                        ),
                        const Text(
                          'ESG',
                          style: TextStyle(
                            color: AppColors.textMuted,
                            fontSize: 10,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _score(Map<String, dynamic> s) async {
    double score = ((s['esgScore'] ?? 0) as num).toDouble();
    final ok = await showAppSheet<bool>(
      context,
      title: '${s['name']} · ESG puanı',
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Text(
                '${score.round()}',
                style: TextStyle(
                  fontSize: 40,
                  fontWeight: FontWeight.w800,
                  color: _esgColor(score.round()),
                ),
              ),
            ),
            Slider(
              value: score,
              min: 0,
              max: 100,
              divisions: 100,
              onChanged: (v) => setLocal(() => score = v),
            ),
            const SizedBox(height: 8),
            ElevatedButton(
              onPressed: () async {
                final r = await CompanyServices.setEsgScore(
                  s['id'],
                  score.round(),
                );
                if (!ctx.mounted) return;
                showToast(
                  ctx,
                  r.isOk ? 'Puan güncellendi.' : r.error!,
                  error: !r.isOk,
                );
                if (r.isOk) Navigator.pop(ctx, true);
              },
              child: const Text('Kaydet'),
            ),
          ],
        ),
      ),
    );
    if (ok == true) _load();
  }

  Future<void> _add() async {
    final name = TextEditingController();
    final email = TextEditingController();
    final ok = await showAppSheet<bool>(
      context,
      title: 'Tedarikçi ekle',
      builder: (ctx) {
        String? error;
        return StatefulBuilder(
          builder: (ctx, setLocal) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextField(
                controller: name,
                decoration: const InputDecoration(labelText: 'Firma adı'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: email,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(
                  labelText: 'İletişim e-postası',
                ),
              ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 10),
                  child: Text(
                    error!,
                    style: const TextStyle(color: AppColors.danger),
                  ),
                ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () async {
                  final r = await CompanyServices.addSupplier(
                    companyId: await _superAdminCompany(),
                    name: name.text.trim(),
                    email: email.text.trim(),
                  );
                  if (!ctx.mounted) return;
                  r.isOk
                      ? Navigator.pop(ctx, true)
                      : setLocal(() => error = r.error);
                },
                child: const Text('Ekle'),
              ),
            ],
          ),
        );
      },
    );
    if (ok == true) _load();
  }
}

// ───────────────────────────── Bildirimler ─────────────────────────────

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<Map<String, dynamic>> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final r = await CompanyServices.notifications();
    if (!mounted) return;
    setState(() {
      _loading = false;
      _error = r.error;
      _items = r.data ?? [];
    });
  }

  @override
  Widget build(BuildContext context) {
    return PageScaffold(
      title: 'Bildirimler',
      subtitle: '${_items.where((n) => n['isRead'] != true).length} okunmamış',
      body: _AsyncList(
        loading: _loading,
        error: _error,
        isEmpty: _items.isEmpty,
        onRefresh: _load,
        empty: const EmptyState(
          icon: Icons.notifications_none_rounded,
          title: 'Bildirim yok',
          message: 'Her şey yolunda görünüyor.',
        ),
        children: [
          for (final n in _items)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: GlassCard(
                borderColor: n['isRead'] == true
                    ? null
                    : AppColors.warning.withValues(alpha: 0.4),
                onTap: n['isRead'] == true
                    ? null
                    : () async {
                        await CompanyServices.markRead(n['id']);
                        _load();
                      },
                padding: const EdgeInsets.all(14),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    IconBadge(
                      icon: Icons.notifications_rounded,
                      color: n['isRead'] == true
                          ? AppColors.textMuted
                          : AppColors.warning,
                      size: 38,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            n['title'] ?? '',
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            n['message'] ?? '',
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 12.5,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            Fmt.relative(n['createdAt']),
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 11,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}

// ───────────────────────────── Denetim kaydı ─────────────────────────────

class AuditLogsScreen extends StatefulWidget {
  const AuditLogsScreen({super.key});

  @override
  State<AuditLogsScreen> createState() => _AuditLogsScreenState();
}

class _AuditLogsScreenState extends State<AuditLogsScreen> {
  List<Map<String, dynamic>> _items = [];
  int _total = 0;
  bool _loading = true;
  String? _error;

  static const _entityLabels = {
    'EmissionEntry': 'Emisyon kaydı',
    'Facility': 'Tesis',
    'AppUser': 'Kullanıcı',
    'Company': 'Şirket',
    'Target': 'Hedef',
    'Supplier': 'Tedarikçi',
    'Document': 'Belge',
    'ApiKey': 'API anahtarı',
    'CarbonCredit': 'Karbon kredisi',
  };

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final r = await CompanyServices.auditLogs(
      companyId: await _superAdminCompany(),
    );
    if (!mounted) return;
    setState(() {
      _loading = false;
      _error = r.error;
      _items = List<Map<String, dynamic>>.from(r.data?['data'] ?? []);
      _total = (r.data?['totalCount'] ?? 0) as int;
    });
  }

  (IconData, Color, String) _action(String a) => switch (a) {
    'Create' => (
      Icons.add_circle_outline_rounded,
      AppColors.primary,
      'oluşturdu',
    ),
    'Delete' => (
      Icons.remove_circle_outline_rounded,
      AppColors.danger,
      'sildi',
    ),
    _ => (Icons.edit_outlined, AppColors.accent, 'güncelledi'),
  };

  @override
  Widget build(BuildContext context) {
    return PageScaffold(
      title: 'Denetim kaydı',
      subtitle: _total > _items.length
          ? 'Son ${_items.length} / $_total işlem'
          : '$_total işlem',
      body: _AsyncList(
        loading: _loading,
        error: _error,
        isEmpty: _items.isEmpty,
        onRefresh: _load,
        empty: const EmptyState(
          icon: Icons.history_rounded,
          title: 'Kayıt yok',
        ),
        children: [
          for (final l in _items)
            Builder(
              builder: (context) {
                final (icon, color, verb) = _action(l['action'] ?? '');
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: GlassCard(
                    onTap: () => _detail(l),
                    padding: const EdgeInsets.all(12),
                    child: Row(
                      children: [
                        Icon(icon, color: color, size: 22),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                '${l['userName']} ${_entityLabels[l['entityName']] ?? l['entityName']} $verb',
                                style: const TextStyle(
                                  fontWeight: FontWeight.w600,
                                  fontSize: 13,
                                ),
                              ),
                              Text(
                                Fmt.dateTime(l['createdAt']),
                                style: const TextStyle(
                                  color: AppColors.textMuted,
                                  fontSize: 11,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const Icon(
                          Icons.chevron_right_rounded,
                          color: AppColors.textMuted,
                          size: 18,
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
        ],
      ),
    );
  }

  void _detail(Map<String, dynamic> l) {
    showAppSheet(
      context,
      title: _entityLabels[l['entityName']] ?? l['entityName'] ?? 'Kayıt',
      builder: (_) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (l['oldValues'] != null) ...[
            const Text(
              'Önce',
              style: TextStyle(
                color: AppColors.danger,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 4),
            SelectableText(
              l['oldValues'],
              style: const TextStyle(fontFamily: 'monospace', fontSize: 11.5),
            ),
            const SizedBox(height: 12),
          ],
          if (l['newValues'] != null) ...[
            const Text(
              'Sonra',
              style: TextStyle(
                color: AppColors.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 4),
            SelectableText(
              l['newValues'],
              style: const TextStyle(fontFamily: 'monospace', fontSize: 11.5),
            ),
          ],
        ],
      ),
    );
  }
}

// ───────────────────────────── Profil / güvenlik ─────────────────────────────

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final _current = TextEditingController();
  final _next = TextEditingController();
  bool _saving = false;
  Map<String, dynamic>? _me;
  Map<String, dynamic>? _ai;

  @override
  void initState() {
    super.initState();
    CompanyServices.me().then(
      (r) => mounted ? setState(() => _me = r.data) : null,
    );
    ApiClient.get('/ai/status').then(
      (r) => mounted && ApiClient.isOk(r)
          ? setState(() => _ai = ApiClient.decode(r))
          : null,
    );
  }

  @override
  void dispose() {
    _current.dispose();
    _next.dispose();
    super.dispose();
  }

  Future<void> _change() async {
    setState(() => _saving = true);
    final r = await AuthService.changePassword(_current.text, _next.text);
    if (!mounted) return;
    setState(() => _saving = false);
    showToast(
      context,
      r.isOk
          ? 'Şifre değiştirildi. Diğer cihazlardaki oturumlar kapatıldı.'
          : r.error!,
      error: !r.isOk,
    );
    if (r.isOk) {
      _current.clear();
      _next.clear();
    }
  }

  @override
  Widget build(BuildContext context) {
    final me = _me;
    return PageScaffold(
      title: 'Profil ve güvenlik',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 40),
        children: [
          GlassCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SectionTitle('Hesap'),
                _info('Ad Soyad', me?['fullName']),
                _info('E-posta', me?['email']),
                _info('Şirket', me?['companyName']),
                _info(
                  'Rol',
                  me == null ? null : UserRoleX.parse(me['role']).label,
                ),
                _info(
                  '2FA',
                  me == null
                      ? null
                      : (me['twoFactorEnabled'] == true ? 'Aktif' : 'Kapalı'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          GlassCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SectionTitle(
                  'Şifre değiştir',
                  subtitle: 'En az 8 karakter, harf ve rakam',
                ),
                TextField(
                  controller: _current,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Mevcut şifre',
                    prefixIcon: Icon(Icons.lock_outline),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _next,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Yeni şifre',
                    prefixIcon: Icon(Icons.lock_reset_rounded),
                  ),
                ),
                const SizedBox(height: 14),
                ElevatedButton(
                  onPressed: _saving ? null : _change,
                  child: const Text('Şifreyi güncelle'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          GlassCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SectionTitle(
                  'Yapay zeka',
                  subtitle: 'Yerel Ollama modelleri',
                ),
                _info(
                  'Durum',
                  _ai == null
                      ? null
                      : (_ai!['available'] == true
                            ? 'Çalışıyor'
                            : (_ai!['error'] ?? 'Çevrimdışı')),
                ),
                _info('Görsel (OCR)', _ai?['visionModel']),
                _info('Metin / asistan', _ai?['textModel']),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _info(String label, dynamic value) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 6),
    child: Row(
      children: [
        SizedBox(
          width: 120,
          child: Text(
            label,
            style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
          ),
        ),
        Expanded(
          child: Text(
            value?.toString() ?? '…',
            style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13),
          ),
        ),
      ],
    ),
  );
}
