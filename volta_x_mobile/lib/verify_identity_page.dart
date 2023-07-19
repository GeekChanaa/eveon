import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:volta_x_mobile/enter_phone_number_page.dart';
import 'package:volta_x_mobile/verify_email_page.dart';

import 'login_page.dart';

class VerifyIdentityPage extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        centerTitle: true,
        title: Text(
          'Verify Identity',
          style: TextStyle(fontWeight: FontWeight.bold, color: Colors.black),
        ),
        elevation: 0,
        iconTheme: IconThemeData(
          color: Colors.black, // Set the color of the arrow here
        ),
      ),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(height: 32),
            Image.asset(
              'assets/images/personalcard.png', // Replace with your image asset path
              height: 100,
              width: 100,
            ),
            SizedBox(height: 16),
            Text(
              'Verify Your Identity',
              style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              textAlign: TextAlign.center,
            ),
            SizedBox(height: 20),
            Text(
              'Browse and build your collection of the world’s most cutting-edge digital art',
              textAlign: TextAlign.center,
            ),
            SizedBox(height: 32),
            Container(
              width: MediaQuery.of(context).size.width * 0.8,
              height: 100, // 80% of screen width
              child: Padding(
                padding: EdgeInsets.symmetric(
                  vertical: 20,
                ), // Top and bottom padding of 20px
                child: ElevatedButton(
                  child: Text(
                    'Verify Email',
                    style: TextStyle(
                      color: Color.fromARGB(255, 1, 1, 1),
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  style: ButtonStyle(
                    elevation: MaterialStateProperty.all<double>(0),
                    backgroundColor: MaterialStateProperty.all<Color>(
                      Color.fromARGB(255, 255, 255, 255),
                    ),
                    shape: MaterialStateProperty.all<RoundedRectangleBorder>(
                      RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(30),
                        side: BorderSide(
                          color: const Color.fromARGB(255, 168, 168, 168),
                        ),
                      ),
                    ),
                  ),
                  onPressed: () {
                    // Handle verify email button press
                    Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => VerifyEmailPage(),
                            ),
                          );
                  },
                ),
              ),
            ),
            Container(
              width: MediaQuery.of(context).size.width * 0.8,
              height: 100, // 80% of screen width
              child: Padding(
                padding: EdgeInsets.symmetric(
                  vertical: 20,
                ), // Top and bottom padding of 20px
                child: ElevatedButton(
                  child: Text(
                    'Verify Phone Number',
                    style: TextStyle(
                      color: Color.fromARGB(255, 1, 1, 1),
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  style: ButtonStyle(
                    elevation: MaterialStateProperty.all<double>(0),
                    backgroundColor: MaterialStateProperty.all<Color>(
                      Color.fromARGB(255, 249, 249, 249),
                    ),
                    shape: MaterialStateProperty.all<RoundedRectangleBorder>(
                      RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(30),
                        side: BorderSide(
                          color: const Color.fromARGB(255, 168, 168, 168),
                        ),
                      ),
                    ),
                  ),
                  onPressed: () {
                    Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => PhoneNumberPage(),
                            ),
                          );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
