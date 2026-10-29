import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'screens/home_screen.dart';
import 'screens/login_screen.dart';
import 'screens/module_screens.dart';
import 'screens/register_screen.dart';
import 'services/api_client.dart';
import 'services/auth_service.dart';
import 'theme/app_theme.dart';
import 'widgets/aurora_background.dart';

final navigatorKey = GlobalKey<NavigatorState>();
final messengerKey = GlobalKey<ScaffoldMessengerState>();

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializeDateFormatting('tr_TR');

  // Refresh token da geçersizse oturumu temizle ve girişe dön (tek sefer).
  var redirecting = false;
  ApiClient.onSessionExpired = () async {
    if (redirecting) return;
    redirecting = true;
    await AuthService.clearLocalSession();
    navigatorKey.currentState?.pushNamedAndRemoveUntil('/login', (_) => false);
    messengerKey.currentState?.showSnackBar(
      const SnackBar(content: Text('Oturumunuzun süresi doldu, lütfen tekrar giriş yapın.')),
    );
    redirecting = false;
  };

  runApp(const GreenOpsApp());
}

class GreenOpsApp extends StatelessWidget {
  const GreenOpsApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'GreenOps CarbonTrace',
      debugShowCheckedModeBanner: false,
      navigatorKey: navigatorKey,
      scaffoldMessengerKey: messengerKey,
      theme: AppTheme.dark,
      locale: const Locale('tr', 'TR'),
      supportedLocales: const [Locale('tr', 'TR')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: const SplashGate(),
      routes: {
        '/login': (context) => const LoginScreen(),
        '/register': (context) => const RegisterScreen(),
        '/home': (context) => const HomeScreen(),
        '/users': (context) => const UsersScreen(),
        '/suppliers': (context) => const SuppliersScreen(),
        '/targets': (context) => const TargetsScreen(),
        '/notifications': (context) => const NotificationsScreen(),
        '/audit-logs': (context) => const AuditLogsScreen(),
        '/settings': (context) => const SettingsScreen(),
      },
    );
  }
}

/// Checks auth state and routes to login or home.
class SplashGate extends StatefulWidget {
  const SplashGate({super.key});

  @override
  State<SplashGate> createState() => _SplashGateState();
}

class _SplashGateState extends State<SplashGate> {
  @override
  void initState() {
    super.initState();
    _checkAuth();
  }

  Future<void> _checkAuth() async {
    final loggedIn = await AuthService.isLoggedIn();
    if (!mounted) return;

    Navigator.of(context).pushReplacementNamed(loggedIn ? '/home' : '/login');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: AuroraBackground(
        child: Center(
          child: Container(
            margin: const EdgeInsets.all(24),
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: AppColors.surface.withValues(alpha: 0.82),
              borderRadius: BorderRadius.circular(24),
              border: Border.all(color: AppColors.border),
            ),
            child: const Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.eco_rounded, color: AppColors.primary, size: 56),
                SizedBox(height: 16),
                Text(
                  'GreenOps CarbonTrace',
                  style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
                ),
                SizedBox(height: 8),
                Text(
                  'Sürdürülebilirlik metrikleri hazırlanıyor',
                  style: TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                SizedBox(height: 24),
                SizedBox(
                  width: 28,
                  height: 28,
                  child: CircularProgressIndicator(
                    strokeWidth: 3,
                    color: AppColors.primary,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
