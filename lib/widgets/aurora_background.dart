import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

class AuroraBackground extends StatelessWidget {
  const AuroraBackground({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [
                  Color(0xFF06131A),
                  Color(0xFF071B24),
                  Color(0xFF081017),
                ],
              ),
            ),
          ),
        ),
        Positioned(
          top: -120,
          left: -60,
          child: _blob(const Color(0xFF30C7FF), 240),
        ),
        Positioned(
          top: 180,
          right: -70,
          child: _blob(const Color(0xFF39D98A), 200),
        ),
        Positioned(
          bottom: -100,
          left: 80,
          child: _blob(const Color(0xFF1F495D), 220),
        ),
        Positioned.fill(
          child: Container(
            decoration: BoxDecoration(
              color: AppColors.background.withValues(alpha: 0.55),
            ),
          ),
        ),
        SafeArea(child: child),
      ],
    );
  }

  Widget _blob(Color color, double size) {
    return IgnorePointer(
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          gradient: RadialGradient(
            colors: [
              color.withValues(alpha: 0.35),
              color.withValues(alpha: 0.01),
            ],
          ),
        ),
      ),
    );
  }
}
