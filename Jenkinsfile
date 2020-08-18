pipeline {
    agent any

    stages {
        stage('Build') {
            steps {
                sh 'Build.cmd unstable'
            }
        }
        stage('Test') {
            steps {
                echo 'Testing..'
            }
        }
        stage('Deploy') {
            steps {
                echo 'Deploying....'
            }
        }
    }
}
