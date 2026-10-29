import 'package:flutter/material.dart';

import '../services/auth_service.dart';
import '../theme/app_theme.dart';
import '../widgets/ui.dart';
import 'assistant_screen.dart';
import 'dashboard_screen.dart';
import 'emission_entry_screen.dart';
import 'facilities_screen.dart';
import 'more_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  static const _emissionsTab = 1;
  static const _assistantTab = 2;

  int _index = 0;
  final _emissionsKey = GlobalKey<EmissionEntryScreenState>();

  late final List<Widget> _screens = [
    DashboardScreen(
      onOpenEmissions: () {
        setState(() => _index = _emissionsTab);
        _emissionsKey.currentState?.showPending();
      },
      onOpenAssistant: () => setState(() => _index = _assistantTab),
    ),
    EmissionEntryScreen(key: _emissionsKey),
    const AssistantScreen(),
    const FacilitiesScreen(),
    MoreScreen(onLogout: _logout),
  ];

  Future<void> _logout() async {
    final ok = await confirmDialog(
      context,
      title: 'Çıkış yap',
      message: 'Oturumunuz bu cihazda kapatılacak.',
      confirmLabel: 'Çıkış yap',
      destructive: true,
    );
    if (!ok) return;
    await AuthService.logout();
    if (!mounted) return;
    Navigator.of(context).pushNamedAndRemoveUntil('/login', (_) => false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBody: true,
      body: IndexedStack(index: _index, children: _screens),
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.fromLTRB(14, 0, 14, 12),
        child: Container(
          height: 70,
          padding: const EdgeInsets.symmetric(horizontal: 6),
          decoration: BoxDecoration(
            color: AppColors.surface.withValues(alpha: 0.96),
            borderRadius: BorderRadius.circular(26),
            border: Border.all(color: AppColors.border),
            boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.35), blurRadius: 24, offset: const Offset(0, 8))],
          ),
          child: Row(
            children: [
              _item(0, Icons.space_dashboard_outlined, Icons.space_dashboard_rounded, 'Panel'),
              _item(1, Icons.cloud_outlined, Icons.cloud_rounded, 'Emisyon'),
              _assistantButton(),
              _item(3, Icons.apartment_outlined, Icons.apartment_rounded, 'Tesisler'),
              _item(4, Icons.grid_view_outlined, Icons.grid_view_rounded, 'Daha fazla'),
            ],
          ),
        ),
      ),
    );
  }

  Widget _item(int index, IconData icon, IconData activeIcon, String label) {
    final active = _index == index;
    return Expanded(
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: () => setState(() => _index = index),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              decoration: BoxDecoration(
                color: active ? AppColors.primary.withValues(alpha: 0.18) : Colors.transparent,
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(active ? activeIcon : icon, size: 22, color: active ? AppColors.primary : AppColors.textMuted),
            ),
            const SizedBox(height: 3),
            Text(
              label,
              maxLines: 1,
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w700,
                color: active ? AppColors.primary : AppColors.textMuted,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _assistantButton() {
    final active = _index == _assistantTab;
    return Expanded(
      child: Center(
        child: GestureDetector(
          onTap: () => setState(() => _index = _assistantTab),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 200),
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: const LinearGradient(colors: [Color(0xFFB18CFF), AppColors.primary], begin: Alignment.topLeft, end: Alignment.bottomRight),
              boxShadow: [
                BoxShadow(color: const Color(0xFFB18CFF).withValues(alpha: active ? 0.6 : 0.3), blurRadius: active ? 18 : 10),
              ],
            ),
            child: const Icon(Icons.auto_awesome_rounded, color: Color(0xFF0A1A12), size: 26),
          ),
        ),
      ),
    );
  }
}
