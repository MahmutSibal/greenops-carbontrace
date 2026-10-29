import 'package:flutter/material.dart';

import '../services/ai_service.dart';
import '../theme/app_theme.dart';
import '../widgets/aurora_background.dart';
import '../widgets/ui.dart';

/// gemma4 ile şirket verisine dayalı karbon asistanı. Sohbet geçmişi sadece bu oturumda tutulur.
class AssistantScreen extends StatefulWidget {
  const AssistantScreen({super.key});

  @override
  State<AssistantScreen> createState() => _AssistantScreenState();
}

class _Msg {
  _Msg(this.role, this.text, {this.error = false});
  final String role; // user | assistant
  final String text;
  final bool error;
}

class _AssistantScreenState extends State<AssistantScreen> {
  static const _suggestions = [
    'En büyük emisyon kaynağımız ne?',
    'Gelecek ay ne kadar emisyon bekleniyor?',
    'Scope 2 emisyonlarını nasıl azaltırız?',
    'CBAM bizi nasıl etkiler?',
  ];

  final _ctrl = TextEditingController();
  final _scroll = ScrollController();
  final List<_Msg> _messages = [];
  bool _sending = false;
  bool? _available;

  @override
  void initState() {
    super.initState();
    AiService.status().then((r) {
      if (mounted) setState(() => _available = r.data?['available'] == true);
    });
  }

  @override
  void dispose() {
    _ctrl.dispose();
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _send([String? preset]) async {
    final text = (preset ?? _ctrl.text).trim();
    if (text.isEmpty || _sending) return;
    _ctrl.clear();

    final history = [
      for (final m in _messages.where((m) => !m.error)) {'role': m.role, 'content': m.text},
    ];
    setState(() {
      _messages.add(_Msg('user', text));
      _sending = true;
    });
    _scrollToEnd();

    final res = await AiService.chat(text, history);
    if (!mounted) return;
    setState(() {
      _sending = false;
      _messages.add(res.isOk ? _Msg('assistant', res.data!) : _Msg('assistant', res.error!, error: true));
    });
    _scrollToEnd();
  }

  void _scrollToEnd() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scroll.hasClients) {
        _scroll.animateTo(_scroll.position.maxScrollExtent, duration: const Duration(milliseconds: 250), curve: Curves.easeOut);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: AuroraBackground(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 8, 8),
              child: Row(
                children: [
                  const IconBadge(icon: Icons.auto_awesome_rounded, color: Color(0xFFB18CFF), size: 40),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Karbon Asistanı', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
                        Row(
                          children: [
                            Container(
                              width: 7,
                              height: 7,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                color: _available == null
                                    ? AppColors.textMuted
                                    : _available! ? AppColors.primary : AppColors.danger,
                              ),
                            ),
                            const SizedBox(width: 6),
                            Expanded(
                              child: Text(
                                _available == null
                                    ? 'Bağlanıyor…'
                                    : _available! ? 'gemma4 · yerel model, bulut AI servisine veri gitmez' : 'AI servisi çevrimdışı',
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(color: AppColors.textMuted, fontSize: 11),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  if (_messages.isNotEmpty)
                    IconButton(
                      tooltip: 'Yeni sohbet',
                      onPressed: _sending ? null : () => setState(_messages.clear),
                      icon: const Icon(Icons.refresh_rounded, color: AppColors.textMuted),
                    ),
                ],
              ),
            ),
            Expanded(
              child: _messages.isEmpty
                  ? _buildWelcome()
                  : ListView.builder(
                      controller: _scroll,
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                      itemCount: _messages.length + (_sending ? 1 : 0),
                      itemBuilder: (_, i) => i == _messages.length ? const _TypingBubble() : _Bubble(_messages[i]),
                    ),
            ),
            _buildInput(),
          ],
        ),
      ),
    );
  }

  Widget _buildWelcome() {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        const SizedBox(height: 12),
        const EmptyState(
          icon: Icons.forum_outlined,
          title: 'Emisyonlarınız hakkında soru sorun',
          message: 'Asistan şirketinizin onaylı verilerini, tahminleri ve hedeflerinizi bilir. Kayıt oluşturamaz veya onaylayamaz.',
        ),
        for (final s in _suggestions)
          Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: GlassCard(
              onTap: () => _send(s),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              child: Row(
                children: [
                  const Icon(Icons.lightbulb_outline_rounded, color: AppColors.warning, size: 18),
                  const SizedBox(width: 10),
                  Expanded(child: Text(s, style: const TextStyle(fontSize: 13.5))),
                  const Icon(Icons.north_east_rounded, color: AppColors.textMuted, size: 16),
                ],
              ),
            ),
          ),
      ],
    );
  }

  Widget _buildInput() {
    return SafeArea(
      top: false,
      minimum: const EdgeInsets.only(bottom: 96),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 6, 12, 0),
        child: Row(
          children: [
            Expanded(
              child: TextField(
                controller: _ctrl,
                minLines: 1,
                maxLines: 4,
                maxLength: 1000,
                textInputAction: TextInputAction.send,
                onSubmitted: (_) => _send(),
                decoration: const InputDecoration(hintText: 'Bir soru yazın…', counterText: ''),
              ),
            ),
            const SizedBox(width: 8),
            IconButton.filled(
              onPressed: _sending ? null : _send,
              style: IconButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: const Color(0xFF052414),
                minimumSize: const Size(50, 50),
              ),
              icon: const Icon(Icons.send_rounded),
            ),
          ],
        ),
      ),
    );
  }
}

class _Bubble extends StatelessWidget {
  const _Bubble(this.msg);

  final _Msg msg;

  @override
  Widget build(BuildContext context) {
    final mine = msg.role == 'user';
    final color = msg.error ? AppColors.danger : (mine ? AppColors.primary : AppColors.surfaceSoft);
    return Align(
      alignment: mine ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.82),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 11),
        decoration: BoxDecoration(
          color: mine ? color.withValues(alpha: 0.2) : color.withValues(alpha: msg.error ? 0.15 : 0.9),
          border: Border.all(color: mine || msg.error ? color.withValues(alpha: 0.45) : AppColors.border),
          borderRadius: BorderRadius.only(
            topLeft: const Radius.circular(18),
            topRight: const Radius.circular(18),
            bottomLeft: Radius.circular(mine ? 18 : 4),
            bottomRight: Radius.circular(mine ? 4 : 18),
          ),
        ),
        child: SelectableText(msg.text, style: const TextStyle(fontSize: 13.5, height: 1.45)),
      ),
    );
  }
}

class _TypingBubble extends StatelessWidget {
  const _TypingBubble();

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        decoration: BoxDecoration(
          color: AppColors.surfaceSoft.withValues(alpha: 0.9),
          border: Border.all(color: AppColors.border),
          borderRadius: BorderRadius.circular(18),
        ),
        child: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFFB18CFF))),
            SizedBox(width: 10),
            Text('Düşünüyor…', style: TextStyle(color: AppColors.textMuted, fontSize: 13)),
          ],
        ),
      ),
    );
  }
}
