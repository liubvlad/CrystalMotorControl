void SerialPrintMsgValue(String msg, float val){
  String strOut = msg + val;
  strOut += strOut + '\n';
  Serial.println(strOut); 
}


void serialEvent() {

  if (Serial.available() > 0) {
    String input = Serial.readStringUntil('\n');

    // Найдем позицию первого пробела
    int spaceIndex = input.indexOf(' ');
    String command;
    
    // Если пробел найден, разделим строку на команду и значение
    if (spaceIndex != -1) {
      command = input.substring(0, spaceIndex);
      //value = input.substring(spaceIndex + 1).toInt();
    } else {
      command = input;
    }
    
    if (command == "start") {
      //start();
    } else if (command == "break") {
      stepper.brake();
    } else if (command == "stop") {
      stepper.stop();
    } else if (command == "left") {
      int32_t stepsLeft = command.substring(spaceIndex + 1).toInt();
      stepper.setTarget(stepsLeft, RELATIVE);
    } else if (command == "right") {
      int32_t stepsRight = command.substring(spaceIndex + 1).toInt();
      stepper.setTarget(-stepsRight, RELATIVE);
    } else if (command == "move") {
      int32_t stepsPos = command.substring(spaceIndex + 1).toInt();
      stepper.setTarget(stepsPos);
    } else if (command == "home") {
      home();
    } else if (command == "targ") {
      int value = input.substring(spaceIndex + 1).toInt();
      SerialPrintMsgValue("curTarg=", value);
    } else if (command == "targDeg") {
      float value = input.substring(spaceIndex + 1).toFloat();
      SerialPrintMsgValue("curTargDeg=", value);
    }
  }

}
