void SerialPrintMsgValue(String msg, float val){
  String strOut = msg + val;
  //strOut = strOut + "\n\r";
  Serial.println(strOut); 
}


void serialEvent() {

  if (Serial.available() > 0) {
    String input = Serial.readStringUntil('\n');

    // Найдем позицию первого пробела
    int spaceIndex = input.indexOf(' ');
    String command, value;
    
    // Если пробел найден, разделим строку на команду и значение
    if (spaceIndex != -1) {
      command = input.substring(0, spaceIndex);
      value = input.substring(spaceIndex + 1);
    } else {
      command = input;
    }
    
    if (command == "ping") {
      Serial.println("pong");
    } else if (command == "break") {
      stepper.brake();
    } else if (command == "stop") {
      stepper.stop();
    } else if (command == "move") {
      int32_t stepsLeft = value.toInt();
      stepper.setTarget(stepsLeft, RELATIVE);
    } else if (command == "moveTo") {
      int32_t stepsPos = value.toInt();
      stepper.setTarget(stepsPos);
    } else if (command == "home") {
      stepper.setTarget(0);
    } else if (command == "sethome") {
      shouldSetHome = true;
      stepper.setTarget(STEPS_ON_TURN, RELATIVE);
    } 
    
    
    else if (command == "targ") {
      int targ = stepper.getTarget();
      SerialPrintMsgValue("targ=", targ);
    } else if (command == "targDeg") {
      float targDeg = stepper.getTargetDeg();
      SerialPrintMsgValue("targDeg=", targDeg);
    }
    
    else if (command == "current") {
      int current = stepper.getCurrent();
      SerialPrintMsgValue("current=", current);
    } else if (command == "currentDeg") {
      float currentDeg = stepper.getCurrentDeg();
      SerialPrintMsgValue("currentDeg=", currentDeg);
    }
  }

}
