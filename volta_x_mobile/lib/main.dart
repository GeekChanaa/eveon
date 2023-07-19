import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'onboarding_page.dart'; // Import the file here

void main() {
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  // Create a method to load the fonts
  Future<void> _loadFonts() async {
    final fontLoader = FontLoader('Urbanist');
    fontLoader.addFont(
      rootBundle.load('fonts/urbanist_regular.ttf'),
    );
    fontLoader.addFont(
      rootBundle.load('fonts/urbanist_bold.ttf'),
    );
    fontLoader.addFont(
      rootBundle.load('fonts/urbanist_medium.ttf'),
    );
    fontLoader.addFont(
      rootBundle.load('fonts/urbanist_semibold.ttf'),
    );
    fontLoader.addFont(
      rootBundle.load('fonts/urbanist_extrabold.ttf'),
    );
    await fontLoader.load();
  }

  @override
  Widget build(BuildContext context) {
    // Call the _loadFonts method to load the fonts
    _loadFonts();

    return MaterialApp(
      title: 'My Flutter App',
      theme: ThemeData(
        primarySwatch: Colors.blue,
        scaffoldBackgroundColor: Colors.white,
        fontFamily: 'Urbanist', // Set the default font family
      ),
      home: OnboardingPage(), // Use your OnboardingPage widget here
    );
  }
}
