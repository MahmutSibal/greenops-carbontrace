import 'package:flutter/material.dart';

import '../services/auth_service.dart';
import '../theme/app_theme.dart';
import '../widgets/aurora_background.dart';
import '../widgets/ui.dart';

/// "Daha fazla" sekmesi: profil özeti ve rol bazlı modül menüsü.
class MoreScreen extends StatefulWidget {
  const MoreScreen({super.key, required this.onLogout});

  final VoidCallback onLogout;

  @override
  State<MoreScreen> createState() => _MoreScreenState();
}

class _MoreScreenState extends State<MoreScreen> {
  String _name = '';
  String _email = '';
  UserRole _role = UserRole.personnel;

  @override
  void initState() {
    super.initState();
    () async {
      final name = await AuthService.getFullName();
      final email = await AuthService.getUserEmail();
      final role = await AuthService.getRole();
      if (!mounted) return;
      setState(() {
        _name = name ?? '';
        _email = email ?? '';
        _role = role;
      });
    }();
  }

  @override
  Widget build(BuildContext context) {
    final items = <(IconData, String, String, String, Color, UserRole)>[
      (Icons.flag_outlined, 'Hedefler', 'Azaltım hedefleri ve ilerleme', '/targets', AppColors.primary, UserRole.personnel),
      (Icons.tune_rounded, 'Azaltım senaryosu', 'Önlemle yılda kaç tCO₂e azalır?', '/scenario', AppColors.accent, UserRole.personnel),
      (Icons.description_outlined, 'Raporlar', 'Yıllık envanter: PDF ve Excel', '/reports', const Color(0xFFFFD166), UserRole.manager),
      (Icons.notifications_none_rounded, 'Bildirimler', 'Size gelen uyarılar', '/notifications', AppColors.warning, UserRole.personnel),
      (Icons.local_shipping_outlined, 'Tedarikçiler', 'Kapsam 3 ve ESG puanları', '/suppliers', AppColors.accent, UserRole.manager),
      (Icons.group_outlined, 'Kullanıcılar', 'Ekip ve roller', '/users', const Color(0xFF8FD14F), UserRole.manager),
      (Icons.history_rounded, 'Denetim kaydı', 'Kim, neyi, ne zaman değiştirdi', '/audit-logs', const Color(0xFFB18CFF), UserRole.companyAdmin),
      (Icons.manage_accounts_outlined, 'Profil ve güvenlik', 'Şifre değiştirme', '/settings', AppColors.textMuted, UserRole.personnel),
    ];

    return Scaffold(
      body: AuroraBackground(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 120),
          children: [
            const Text('Daha fazla', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
            const SizedBox(height: 14),
            GlassCard(
              gradient: LinearGradient(colors: [AppColors.primary.withValues(alpha: 0.16), AppColors.surface]),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 26,
                    backgroundColor: AppColors.primary.withValues(alpha: 0.2),
                    child: Text(
                      _name.isEmpty ? '?' : _name.trim().split(RegExp(r'\s+')).take(2).map((p) => p[0]).join().toUpperCase(),
                      style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.w800, fontSize: 18),
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(_name, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
                        Text(_email, style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                        const SizedBox(height: 6),
                        StatusChip(label: _role.label, color: AppColors.primary),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 18),
            for (final (icon, title, subtitle, route, color, minRole) in items)
              if (_role.atLeast(minRole))
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: GlassCard(
                    onTap: () => Navigator.pushNamed(context, route),
                    padding: const EdgeInsets.all(14),
                    child: Row(
                      children: [
                        IconBadge(icon: icon, color: color),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(title, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14.5)),
                              Text(subtitle, style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
                      ],
                    ),
                  ),
                ),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              onPressed: widget.onLogout,
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.danger,
                side: BorderSide(color: AppColors.danger.withValues(alpha: 0.4)),
                minimumSize: const Size.fromHeight(50),
              ),
              icon: const Icon(Icons.logout_rounded),
              label: const Text('Çıkış yap'),
            ),
          ],
        ),
      ),
    );
  }
}
